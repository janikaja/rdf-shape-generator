using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;

public partial class _Default : Page
{
    public string targetSubject, result = "", error = "", encodedSchema, encodedData;
    public bool additionalInfoRequired = false, shapeReady = false, hasNumericValues = false;
    public List<Property> foundProperties = new List<Property>();

    private Dictionary<string, string> prefixDictionary = new Dictionary<string, string>();
    private Dictionary<string, List<string>> propertyDictionary = new Dictionary<string, List<string>>();
    private string lastTestedPropertyValue;
    private char[] removeSymbols = { ';', '.', ' ', '\t' }, removeBrackets = { '{', '}' };
    private Regex quoteRegex = new Regex(@"""[^""\\]*""");

    protected void Page_Load(object sender, EventArgs e)
    {
        /*if (Request.Params["__EVENTTARGET"] != null && (Request.Params["__EVENTTARGET"].ToString().Contains("dataToClipboard") || Request.Params["__EVENTTARGET"].ToString().Contains("shapeToClipboard")))
        {
            return;
        }*/

        if (SampleList.SelectedIndex < 1)
        {
            SampleList.Items.Clear();
            SampleList.Items.Add(new ListItem("", "0"));
            for (int j = 1; j <= 5; j++)
            {
                SampleList.Items.Add(new ListItem("Sample" + j.ToString(), j.ToString()));
            }
        }

        if (source.Text.Length == 0)
        {
            infoRequired.Value = "0";
            if (Page.IsPostBack && SampleList.SelectedIndex < 1)
            {
                error = "No RDF data!";
            }
            return;
        }

        string[] propertyParts, stringParts, sourceLines = source.Text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
        int i, min, max, start = 0, breakLine = 0, startNode = 1, subjectCounter = 0, propertyCounter = 0, cardinalityCounter = 0, samples = 1, cardinalityIndex, offset = 0;
        string property, record = "", currentProperty = "", cardinality = "", tmp = "", valueSet = "", requestedMin, requestedMax;
        bool firstTime = true, newSample = true, prevWasChecked = false, currIsChecked, countListItems, repeatedProperty = false;
        Result test;
        Range range;

        Graph g = new Graph();
        TurtleParser parser = new TurtleParser();
        try
        {
            parser.Load(g, new StringReader(source.Text));
            //StringParser.Parse(g, source.Text);
        }
        catch (RdfParseException parseEx)
        {
            //This indicates a parser error e.g unexpected character, premature end of input, invalid syntax etc.
            error = "Parser error: ";
            error += parseEx.Message;
            return;
        }
        catch (RdfException rdfEx)
        {
            //This represents a RDF error e.g. illegal triple for the given syntax, undefined namespace
            error = "RDF error: ";
            error += rdfEx.Message;
            return;
        }

        /*result = (g.IsEmpty)? "empty graph" : "not empty graph";
        foreach (ILiteralNode u in g.Nodes.LiteralNodes())
        {
            tmp = u.ToString() + Environment.NewLine;
        }
        foreach (Triple t in g.Triples)
        {
            tmp += t.ToString() + Environment.NewLine;
        }
        result = tmp;
        return;*/

        TripleStore store = new TripleStore();
        store.Add(g);

        //Create a dataset for our queries to operate over
        //We need to explicitly state our default graph or the unnamed graph is used
        //Alternatively you can set the second parameter to true to use the union of all graphs
        //as the default graph
        InMemoryDataset ds = new InMemoryDataset(store, true);

        //Get the Query processor
        ISparqlQueryProcessor processor = new LeviathanQueryProcessor(ds);

        //Use the SparqlQueryParser to give us a SparqlQuery object
        //Should get a Graph back from a CONSTRUCT query
        SparqlQueryParser sparqlparser = new SparqlQueryParser();

        /*SparqlQuery query = sparqlparser.ParseFromString("CONSTRUCT { ?s ?p ?o } WHERE {?s ?p ?o}");
        SparqlQuery query = sparqlparser.ParseFromString("SELECT * WHERE {?s ?p ?o}");
        string q = "PREFIX dct: <http://purl.org/dc/terms/> SELECT ?s (COUNT(?o) AS ?count) WHERE { ?s dct:isPartOf ?o . } GROUP BY ?s";*/

        string q = "PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> SELECT ?o (COUNT(?o) AS ?count) WHERE { ?s rdf:type ?o . } GROUP BY ?o";
        SparqlQuery query = sparqlparser.ParseFromString(q);
        Object results = processor.ProcessQuery(query);

        /*if (results is IGraph)
        {
            IGraph gg = (IGraph)results;
            TurtleFormatter formatter = new TurtleFormatter();
            foreach (Triple t in gg.Triples)
            {
                tmp += t.ToString(formatter) + Environment.NewLine;
            }
            tmp += "Query took " + query.QueryExecutionTime + " milliseconds";
        }*/

        if (infoRequired.Value == "1" && nodeOptions.SelectedValue.Length == 0)
        {
            error = "Please, select class instance!";
        }
        if (results is SparqlResultSet)
        {
            //Print out the Results
            SparqlResultSet rset = (SparqlResultSet)results;
            if (rset.Count > 1)
            {
                additionalInfoRequired = true;
                if (nodeOptions.SelectedValue.Length == 0)
                {
                    i = 0;
                    nodeOptions.Items.Clear();
                    foreach (SparqlResult result in rset)
                    {
                        i++;
                        stringParts = result.ToString().Split(',');
                        nodeOptions.Items.Add(new ListItem(stringParts[0].Substring(5), i.ToString()));
                    }
                }
                if (infoRequired.Value == "0" || (infoRequired.Value == "1" && nodeOptions.SelectedValue.Length == 0))
                {
                    infoRequired.Value = "1";
                    return;
                }
            }
        }

        while(start < sourceLines.Length - 1 && sourceLines[start].Length == 0)
        {
            start++;
        }
        if (sourceLines.Length == 0 || start == sourceLines.Length - 1)
        {
            error = "Incorrect RDF data format!";
            return;
        }

        if (nodeOptions.SelectedValue.Length > 0)
        {
            startNode = int.Parse(nodeOptions.SelectedValue);
        }

        for (i = start; i < sourceLines.Length; i++)
        {
            if (sourceLines[i].Length == 0)
            {
                if (breakLine == 0)
                {
                    breakLine = i;
                }
                subjectCounter++;
                if (subjectCounter == startNode)
                {
                    break;
                }
            }
        }

        if (!checkPrefixes(start, breakLine, sourceLines))
        {
            error = "Incorrect RDF data format!";
        }
        else if (sourceLines.Length < i + 3)
        {
            error = "Incomplete RDF data!";
        }
        else
        {
            targetSubject = HttpUtility.UrlEncode(sourceLines[i + 1].Trim(removeSymbols));
            record += Environment.NewLine + "my:Schema {";
            for (int k = i + 2; k < sourceLines.Length; k++)
            {
                property = sourceLines[k].Trim(removeSymbols);
                propertyParts = splitAndClear(sourceLines[k]);
                if (propertyParts.Length == 1)
                {
                    continue;
                }
                cardinalityCounter++;
                propertyCounter++;
                tmp = "";
                currIsChecked = false;

                if (property.Length > 0)
                {
                    currIsChecked = isPropertyChecked(propertyParts[0]);
                    if (!currIsChecked)
                    {
                        test = hasLanguageTag(property);
                        if (test.Answer == true)
                        {
                            tmp = propertyParts[0] + " [" + test.Contents.TrimStart('"') + "]";
                        }
                        else if ((test = hasStringProperty(property)).Answer == true)
                        {
                            tmp = propertyParts[0] + " xsd:string";
                            repeatedProperty = (test.Cardinality - 1 > offset);
                        }
                        else if (hasDateProperty(propertyParts[1]))
                        {
                            tmp = propertyParts[0] + " xsd:date";
                            repeatedProperty = (property.Split(',').Length - 1 > offset);
                        }
                        else if (hasIntegerProperty(property))
                        {
                            tmp = propertyParts[0] + " xsd:integer";
                            hasNumericValues = true;
                            repeatedProperty = (property.Split(',').Length - 1 > offset);
                        }
                        else if (hasDecimalProperty(propertyParts[1]))
                        {
                            tmp = propertyParts[0] + " xsd:decimal";
                            hasNumericValues = true;
                            repeatedProperty = (property.Split(',').Length - 1 > offset);
                        }
                        else if (hasIriProperty(property))
                        {
                            tmp = propertyParts[0] + " IRI";
                            repeatedProperty = (property.Split(',').Length - 1 > offset);
                        }
                        else
                        {
                            test = hasIRIWithPrefix(propertyParts[1]);
                            if (test.Answer == true)
                            {
                                tmp = propertyParts[0] + ((propertyParts[0] == "a" || propertyParts[0] == "rdf:type") ? " [" + test.Contents + "]" : " IRI");
                                repeatedProperty = (property.Split(',').Length - 1 > offset);
                            }
                        }

                        if (tmp.Length == 0)
                        {
                            error = "Unrecognized property's value! (" + lastTestedPropertyValue + ")";
                            break;
                        }
                    }
                    else
                    {
                        tmp = propertyParts[0];
                    }
                }

                if (k == i + 2 || newSample)
                {
                    currentProperty = tmp;
                    newSample = false;
                    if (currIsChecked)
                    {
                        valueSet = property.Substring(tmp.Length + 1);
                        prevWasChecked = true;
                    }
                }
                else if (currIsChecked)
                {
                    if (firstTime && cardinalityCounter > 1)
                    {
                        firstTime = false;
                        cardinalityCounter--;
                    }
                    if (!prevWasChecked)
                    {
                        saveProperty(currentProperty, cardinalityCounter.ToString());
                        currentProperty = tmp;
                        valueSet = property.Substring(tmp.Length + 1);
                        prevWasChecked = true;
                        cardinalityCounter = 0;
                    }
                    else if (currentProperty == tmp)
                    {
                        valueSet += " " + property.Substring(tmp.Length + 1);
                    }
                    else
                    {
                        saveProperty(currentProperty, valueSet);
                        currentProperty = tmp;
                        valueSet = property.Substring(tmp.Length + 1);
                    }
                    if (tmp.Length == 0)
                    {
                        break;
                    }
                }
                if ((!currIsChecked && currentProperty != tmp) || (k == sourceLines.Length - 1 && !repeatedProperty))
                {
                    if (prevWasChecked)
                    {
                        saveProperty(currentProperty, valueSet);
                        currentProperty = tmp;
                        valueSet = "";
                        cardinalityCounter = 1;
                    }
                    if (firstTime && cardinalityCounter > 1)
                    {
                        firstTime = false;
                        cardinalityCounter--;
                    }
                    if (currentProperty == tmp && k == sourceLines.Length - 1 && !repeatedProperty && !firstTime)
                    {
                        cardinalityCounter++;
                    }
                    if (cardinalityCounter > 1)
                    {
                        cardinality = "{" + cardinalityCounter + "}";
                    }
                    //record += "\t" + currentProperty + cardinality + ";" + Environment.NewLine;
                    if (currentProperty.Length > 0 && !prevWasChecked)
                    //if (currentProperty.Length > 0)
                    {
                        saveProperty(currentProperty, cardinalityCounter.ToString());
                    }
                    if (tmp.Length == 0)
                    {
                        if (nodeOptions.SelectedValue.Length == 0 && k + 1 <= sourceLines.Length - 1 && sourceLines[k + 1].Trim(removeSymbols).Length > 0)
                        {
                            samples++;
                            currentProperty = "";
                            cardinalityCounter = 0;
                            cardinality = "";
                            firstTime = true;
                            newSample = true;
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (currentProperty != tmp && k == sourceLines.Length - 1 && !repeatedProperty)
                    {
                        //record += "\t" + tmp + Environment.NewLine;
                        if (tmp.Length > 0)
                        {
                            currentProperty = tmp;
                        }
                        saveProperty(currentProperty, "1");
                    }
                    currentProperty = tmp;
                    cardinalityCounter = 0;
                    cardinality = "";
                    prevWasChecked = false;
                }
                if (repeatedProperty)
                {
                    offset++;
                    k--;
                }
                else
                {
                    offset = 0;
                }
            }
            i = 0;
            foreach (string key in propertyDictionary.Keys)
            {
                requestedMin = "";
                requestedMax = "";
                tmp = "";
                cardinalityIndex = 0;
                countListItems = false;
                currIsChecked = isPropertyChecked(key, true);
                cardinality = (currIsChecked) ? "valueSet" : getRequestedCardinality(i);
                if (cardinality == "error" || cardinality == "N/A" || cardinality == "valueSet")
                {
                    if (cardinality == "valueSet")
                    {
                        cardinality = "[" + filterUnique(propertyDictionary[key]) + "]";
                        countListItems = true;
                        tmp = getRequestedCardinality(i);
                        if (tmp.Length > 0 && tmp != "N/A")
                        {
                            cardinality += tmp;
                            stringParts = tmp.Trim(removeBrackets).Split(',');
                            if (stringParts.Length == 2)
                            {
                                requestedMin = stringParts[0];
                                requestedMax = stringParts[1];
                                cardinalityIndex = 0;
                            }
                            else if (stringParts[0] == "?")
                            {
                                requestedMin = "0";
                                requestedMax = "1";
                                cardinalityIndex = 2;
                            }
                            else if (stringParts[0] == "*")
                            {
                                requestedMin = "0";
                                cardinalityIndex = 3;
                            }
                            else if (stringParts[0] == "+")
                            {
                                requestedMin = "1";
                                cardinalityIndex = 4;
                            }
                            else
                            {
                                requestedMin = requestedMax = stringParts[0];
                                cardinalityIndex = (requestedMin == "1") ? 1 : 0;
                            }
                        }
                        else if (tmp.Length == 0)
                        {
                            cardinalityIndex = 1;
                        }
                    }
                    else
                    {
                        cardinality = "";
                    }

                    if (cardinalityIndex == 0 && (tmp.Length == 0 || tmp == "N/A"))
                    {
                        if (propertyDictionary[key].Count < samples)
                        {
                            min = 0;
                        }
                        else
                        {
                            min = getMinValue(propertyDictionary[key], countListItems);
                        }
                        max = getMaxValue(propertyDictionary[key], countListItems);

                        if (min == 0 && max == 1)
                        {
                            cardinality += "?";
                        }
                        else if (min == 0 && max > 1)
                        {
                            cardinality += "*";
                        }
                        else if (min == 1 && max > 1)
                        {
                            cardinality += "+";
                        }
                        else if (min == max && min != 1 && key[key.Length - 1] != ']')
                        {
                            cardinality += "{" + min + "}";
                        }
                        else if (min < max)
                        {
                            cardinality += "{" + min + "," + max + "}";
                        }
                    }

                        tmp = "";

                }
                else if (cardinality.Length > 0)
                {
                    stringParts = cardinality.Trim(removeBrackets).Split(',');
                    if (stringParts.Length == 2)
                    {
                        requestedMin = stringParts[0];
                        requestedMax = stringParts[1];
                        cardinalityIndex = 0;
                    }
                    else if (stringParts[0] == "?")
                    {
                        requestedMin = "0";
                        requestedMax = "1";
                        cardinalityIndex = 2;
                    }
                    else if (stringParts[0] == "*")
                    {
                        requestedMin = "0";
                        cardinalityIndex = 3;
                    }
                    else if (stringParts[0] == "+")
                    {
                        requestedMin = "1";
                        cardinalityIndex = 4;
                    }
                    else
                    {
                        requestedMin = requestedMax = stringParts[0];
                        cardinalityIndex = (requestedMin == "1") ? 1 : 0;
                    }
                }

                range = getRange(i, Property.hasDecimalProperty2(key));
                if (!currIsChecked && !range.Is_empty)
                {
                    if (range.Min_value.Length > 0)
                    {
                        tmp = " " + range.Min_type + " " + range.Min_value;
                    }
                    if (range.Max_value.Length > 0)
                    {
                        tmp += " " + range.Max_type + " " + range.Max_value;
                    }
                }

                record += Environment.NewLine + "\t" + key + tmp + ((cardinality.Length > 0) ? " " : "") + cardinality + ";";
                foundProperties.Add(new Property(i, HttpUtility.HtmlEncode(key + tmp + ((cardinality.Length > 0) ? " " : "") + cardinality), currIsChecked, requestedMin, requestedMax, cardinalityIndex, range));
                i++;
            }
            record = record.TrimEnd(';') + Environment.NewLine;
            record += "}" + Environment.NewLine;
        }

        if (error.Length == 0)
        {
            addPrefixes(record);
        }
    }

    private bool checkPrefixes(int start, int breakLine, string[] text)
    {
        string[] prefixParts;
        for (int i = start; i < breakLine; i++)
        {
            prefixParts = splitAndClear(text[i]);
            if (prefixParts.Length < 3 || prefixParts.Length > 4 || (prefixParts[0] != "PREFIX" && prefixParts[0] != "@prefix"))
            {
                return false;
            }
            savePrefix(prefixParts[1].TrimEnd(':'), prefixParts[2]);
        }
        return true;
    }

    private Result hasStringProperty(string property)
    {
        //Regex quoteRegex = new Regex(@"""[^""\\]*(?:\\.[^""\\]*)*""$");
        MatchCollection matches = quoteRegex.Matches(property);
        lastTestedPropertyValue = property;
        if (matches.Count > 0)
        {
            string tmp = "";
            foreach (Match match in matches)
            {
                if (
                    (property.IndexOf("^^xsd:date") != -1 && hasDateProperty(match.ToString() + "^^xsd:date"))
                    ||
                    property.IndexOf("^^xsd:integer") == property.Length - 13
                    ||
                    property.IndexOf("^^xsd:decimal") == property.Length - 13
                )
                {
                    return new Result(false, "N/A");
                }
                tmp += match.Value;
            }
            return new Result(true, tmp, matches.Count);
        }
        return new Result(false, "N/A");
    }

    private bool hasIntegerProperty(string property)
    {
        Regex integerRegex = new Regex(@"\s(\+|-)?\d+$");
        MatchCollection matches = integerRegex.Matches(property);
        lastTestedPropertyValue = property;
        return (matches.Count > 0 || (property.Length > 13 && property.IndexOf("^^xsd:integer") == property.Length - 13));
    }

    private bool hasDecimalProperty(string property)
    {
        Regex decimalRegex = new Regex(@"^(\+|-)?([0-9]+(\.[0-9]*)?|\.[0-9]+),?(\s(\+|-)?([0-9]+(\.[0-9]*)?|\.[0-9]+))*$");
        MatchCollection matches = decimalRegex.Matches(property);
        lastTestedPropertyValue = property;
        return (matches.Count > 0 || (property.Length > 13 && property.IndexOf("^^xsd:decimal") == property.Length - 13));
    }

    private bool hasIriProperty(string property)
    {
        Regex iriRegex = new Regex(@"<(.*)>");
        MatchCollection matches = iriRegex.Matches(property);
        lastTestedPropertyValue = property;
        return (matches.Count > 0);
    }

    private bool hasDateProperty(string property)
    {
        Regex dateRegex = new Regex(@"""(([1-9][0-9]|[1-9])\d{2})-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])""\^\^xsd:date,?(""(([1-9][0-9]|[1-9])\d{2})-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])""\^\^xsd:date)*");
        MatchCollection matches = dateRegex.Matches(property);
        lastTestedPropertyValue = property;
        return (matches.Count > 0);
    }

    private Result hasIRIWithPrefix(string property)
    {
        Regex prefixRegex = new Regex(@"^[a-z]+:[a-zA-Z]+\d*,?(\s[a-z]+:[a-zA-Z]+\d*)*$");
        MatchCollection matches = prefixRegex.Matches(property);
        if (matches.Count > 0)
        {
            string tmp = "";
            foreach (Match match in matches)
            {
                tmp += match.Value;
            }
            return new Result(true, tmp);
        }
        return new Result(false, "N/A");
    }

    private Result hasLanguageTag(string property)
    {
        Regex tagRegex = new Regex(@"[""\\]@[a-z]{2}$");
        MatchCollection matches = tagRegex.Matches(property);
        if (matches.Count > 0)
        {
            string tmp = "";
            foreach (Match match in matches)
            {
                tmp += match.Value + "~";
            }
            return new Result(true, tmp);
        }
        return new Result(false, "N/A");
    }

    private string getPrefix(string key)
    {
        if (prefixDictionary.ContainsKey(key))
        {
            return prefixDictionary[key];
        }

        string[] keys = { "foaf", "xsd", "my", "rdf" };
        string[] values = { "<http://xmlns.com/foaf/0.1/>", "<http://www.w3.org/2001/XMLSchema#>", "<http://my.example/#>", "<http://www.w3.org/1999/02/22-rdf-syntax-ns#>" };
        int index = Array.IndexOf(keys, key);
        if (index != -1)
        {
            return values[index];
        }
        return "";
    }

    private void savePrefix(string key, string value)
    {
        if (!prefixDictionary.ContainsKey(key))
        {
            prefixDictionary[key] = value;

        }
    }

    private void addPrefixes(string shape)
    {
        Regex prefixRegex = new Regex(@"[a-z]+:");
        MatchCollection matches = prefixRegex.Matches(shape);
        if (matches.Count > 0)
        {
            string value, stringMatch, prefixes = "";
            List<string> addedPrefixes = new List<string>();
            foreach (Match match in matches)
            {
                stringMatch = match.ToString();
                value = getPrefix(stringMatch.TrimEnd(':'));
                if (value.Length > 0 && !addedPrefixes.Contains(stringMatch))
                {
                    prefixes += "PREFIX " + match + " " + value + Environment.NewLine;
                    addedPrefixes.Add(stringMatch);
                }
            }
            resultText.Text = prefixes + shape;
            encodedSchema = HttpUtility.UrlEncode(resultText.Text).Replace("+", "%20");
            encodedData = HttpUtility.UrlEncode(source.Text.Trim()).Replace("+", "%20");
            shapeReady = true;
        }
    }

    private string[] splitAndClear(string text)
    {
        string trimmedString = text.Trim(removeSymbols);
        string[] stringParts = trimmedString.Split(' ');
        return Array.FindAll(stringParts, part => part.Length > 0);
    }

    private int extractNumber(string text)
    {
        Regex reg = new Regex(@"=\s\d+\^\^");
        MatchCollection matches = reg.Matches(text);
        if (matches.Count > 0)
        {
            string tmp = "";
            foreach (Match match in matches)
            {
                tmp += match.Value;
                break;
            }
            return int.Parse(tmp.Substring(2, tmp.Length - 4));
        }
        return 0;
    }

    private void saveProperty(string key, string value)
    {
        if (!propertyDictionary.ContainsKey(key))
        {
            List<string> list = new List<string>();
            list.Add(value);
            propertyDictionary[key] = list;
        }
        else
        {
            propertyDictionary[key].Add(value);
        }
    }

    private bool isPropertyChecked(string property, bool splitBoth = false)
    {
        if (!Page.IsPostBack || Request.Form["getValueSet[]"] == null)
        {
            return false;
        }
        string[] stringParts1, stringParts2, checkedProperties = Request.Form["getValueSet[]"].Split(',');
        foreach (string item in checkedProperties)
        {
            stringParts1 = item.Split(' ');
            if (!splitBoth)
            {
                if (stringParts1[0] == property)
                {
                    return true;
                }
            }
            else
            {
                stringParts2 = property.Split(' ');
                if (stringParts1[0] == stringParts2[0])
                {
                    return true;
                }
            }
        }
        return false;
    }

    private string getRequestedCardinality(int index)
    {
        if (
            !Page.IsPostBack
            ||
            (
                (Request.Form["min" + index] == null || Request.Form["min" + index].Length == 0)
                &&
                (Request.Form["cardinality" + index] == null || Request.Form["cardinality" + index] == "0")
            )
        )
        {
            return "N/A";
        }

        int min = -1, max = -1, result;
        string cardinality = "";

        if (Request.Form["min" + index].Length > 0)
        {
            if ((result = getIntegerValue(Request.Form["min" + index])) >= 0)
            {
                min = result;
            }
        }

        if (Request.Form["max" + index].Length > 0)
        {
            if ((result = getIntegerValue(Request.Form["max" + index])) > 0)
            {
                max = result;
            }
            else if (result == 0)
            {
                error = "Maximum cardinality cannot be 0!";
                return "error";
            }
        }

        if (min == -1 && max == -1)
        {
            switch (Request.Form["cardinality" + index])
            {
                case "1":
                    min = max = 1;
                    break;
                case "2":
                    min = 0;
                    max = 1;
                    break;
                case "3":
                    min = 0;
                    break;
                case "4":
                    min = 1;
                    break;
                default:
                    break;
            }
        }

        if (min == 0 && max == 1)
        {
            cardinality = "?";
        }
        else if (min == 0 && max == -1)
        {
            cardinality = "*";
        }
        else if (min == 1 && max == -1)
        {
            cardinality = "+";
        }
        else if (min == max && min > 1)
        {
            cardinality = "{" + min + "}";
        }
        else if (min > 0 && max == -1)
        {
            cardinality = "{" + min + ",}";
        }
        else if (min == -1 && max > 0)
        {
            cardinality = "{0," + max + "}";
        }
        else if (min < max)
        {
            cardinality = "{" + min + "," + max + "}";
        }
        else if (min > max)
        {
            error = "Incorrect cardinalities!";
            return "error";
        }

        return cardinality;
    }

    private int getIntegerValue(string input)
    {
        int number;
        if (Int32.TryParse(input, out number))
        {
            return number;
        }
        return -1;
    }

    private Range getRange(int index, bool floatValue = false)
    {
         if (
            !Page.IsPostBack
            ||
            (
                (Request.Form["minValue" + index] == null || Request.Form["minValue" + index].Length == 0)
                &&
                (Request.Form["maxValue" + index] == null || Request.Form["maxValue" + index].Length == 0)
            )
        )
        {
            return new Range();
        }

        int number, minValueInt = 0, maxValueInt = 0;
        float minValueFloat = 0, maxValueFloat = 0;
        string minValue = "", maxValue = "", minType = Request.Form["valueRangeMin" + index], maxType = Request.Form["valueRangeMax" + index];
        bool noMinValue = (Request.Form["minValue" + index].Length == 0);
        bool noMaxValue = (Request.Form["maxValue" + index].Length == 0);

        if (!floatValue)
        {
            if (Int32.TryParse(Request.Form["minValue" + index], out number))
            {
                minValueInt = number;
            }
            if (Int32.TryParse(Request.Form["maxValue" + index], out number))
            {
                maxValueInt = number;
            }
            if (!noMinValue && !noMaxValue && minValueInt > maxValueInt)
            {
                error = "Incorrect value range!";
                return new Range();
            }
            if (!noMinValue)
            {
                minValue = minValueInt.ToString();
            }
            if (!noMaxValue)
            {
                maxValue = maxValueInt.ToString();
            }
        }
        else
        {
            try
            {
                minValueFloat = float.Parse(Request.Form["minValue" + index], CultureInfo.InvariantCulture);
                maxValueFloat = float.Parse(Request.Form["maxValue" + index], CultureInfo.InvariantCulture);
            }
            catch(Exception ex)
            {
                error = "Incorrect value range!";
                return new Range();
            }

            if (!noMinValue && !noMaxValue && minValueFloat > maxValueFloat)
            {
                error = "Incorrect value range!";
                return new Range();
            }
            if (!noMinValue)
            {
                minValue = minValueFloat.ToString().Replace(',', '.');
            }
            if (!noMaxValue)
            {
                maxValue = maxValueFloat.ToString().Replace(',', '.');
            }
        }
        
        return new Range(minType, minValue, maxType, maxValue);
    }

    private int getMinValue(List<string> list, bool countListItems)
    {
        int min, quotes;
        char separator;
        if (countListItems)
        {
            quotes = countQuotes(list.First());
            separator = (quotes == 0 && list.First().Split(',').Length == 1) ? ' ' : ',';
            min = (quotes > 0)? quotes : list.First().Split(separator).Length;
            foreach (string item in list)
            {
                separator = (quotes == 0 && item.Split(',').Length == 1) ? ' ' : ',';
                if ((quotes = countQuotes(item)) > 0)
                {
                    if (quotes < min)
                    {
                        min = quotes;
                    }
                }
                else if (item.Split(separator).Length < min)
                {
                    min = item.Split(separator).Length;
                }
            }
        }
        else
        {
            min = int.Parse(list.First());
            foreach (string item in list)
            {
                if (int.Parse(item) < min)
                {
                    min = int.Parse(item);
                }
            }
        }
        return min;
    }

    private int getMaxValue(List<string> list, bool countListItems)
    {
        int max, quotes;
        char separator;
        if (countListItems)
        {
            quotes = countQuotes(list.First());
            separator = (quotes == 0 && list.First().Split(',').Length == 1) ? ' ' : ',';
            max = (quotes > 0) ? quotes : list.First().Split(separator).Length;
            foreach (string item in list)
            {
                separator = (quotes == 0 && item.Split(',').Length == 1) ? ' ' : ',';
                if ((quotes = countQuotes(item)) > 0)
                {
                    if (quotes > max)
                    {
                        max = quotes;
                    }
                }
                else if (item.Split(separator).Length > max)
                {
                    max = item.Split(separator).Length;
                }
            }
        }
        else
        {
            max = int.Parse(list.First());
            foreach (string item in list)
            {
                if (int.Parse(item) > max)
                {
                    max = int.Parse(item);
                }
            }
        }
        return max;
    }

    private string filterUnique(List<string> list)
    {
        MatchCollection matches;
        List<string> newList = new List<string>();
        char separator;

        foreach (string item in list)
        {
            matches = quoteRegex.Matches(item);
            if (matches.Count > 0 && item.IndexOf("^^xsd:date") == -1 && item.IndexOf("^^xsd:integer") == -1 && item.IndexOf("^^xsd:decimal") == -1)
            {
                foreach (Match match in matches)
                {
                    newList.Add(match.ToString());
                }
            }
            else if (hasIntegerProperty(item) || hasIriProperty(item) || hasDecimalProperty(item) || hasIRIWithPrefix(item).Answer || item.IndexOf("^^xsd:date") != -1 || item.IndexOf("^^xsd:integer") != -1 || item.IndexOf("^^xsd:decimal") != -1)
            {
                separator = (item.Split(',').Length == 1) ? ' ' : ',';
                foreach (string number in item.Split(separator))
                {
                    newList.Add(number.Trim());
                }
            }
            else
            {
                newList.Add(item);
            }
        }

        return String.Join(" ", newList.Distinct().ToArray());
    }

    private int countQuotes(string text)
    {
        MatchCollection matches = quoteRegex.Matches(text);
        return matches.Count;
    }

    private int countIntegers(string text)
    {
        Regex integerRegex = new Regex(@"\s\d+$");
        MatchCollection matches = integerRegex.Matches(text);
        return matches.Count;
    }

    protected void SampleList_SelectedIndexChanged(object sender, EventArgs e)
    {
        resultText.Text = "";
        source.Text = Sample.getSample(SampleList.SelectedIndex);
        foundProperties.Clear();
        nodeOptions.Items.Clear();
        infoRequired.Value = "0";
        additionalInfoRequired = false;
        shapeReady = false;
        error = "";
    }

    /*protected void dataToClipboard_Click(object sender, EventArgs e)
    {
        Thread thread = new Thread(() => Clipboard.SetText(source.Text));
        thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
        thread.Start();
        thread.Join(); //Wait for the thread to end
    }

    protected void shapeToClipboard_Click(object sender, EventArgs e)
    {
        Thread thread = new Thread(() => Clipboard.SetText(resultText.Text));
        thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
        thread.Start();
        thread.Join(); //Wait for the thread to end
    }*/
}