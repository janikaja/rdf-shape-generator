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
    public bool additionalInfoRequired = false, shapeReady = false, hasNumericValues = false, hideValidatorLink = false;
    public List<Property> foundProperties = new List<Property>();

    private Dictionary<string, string> prefixDictionary = new Dictionary<string, string>();
    private Dictionary<string, List<string>> propertyDictionary = new Dictionary<string, List<string>>();
    private string lastTestedPropertyValue;
    private char[] removeSymbols = { ';', '.', ' ', '\t' }, removeBrackets = { '{', '}' };
    private Regex quoteRegex = new Regex(@"""[^""\\]*""");
    private Graph g = new Graph();
    private SparqlQueryParser sparqlparser = new SparqlQueryParser();
    private ISparqlQueryProcessor processor;

    protected void Page_Load(object sender, EventArgs e)
    {
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

        string[] stringParts, sourceLines = source.Text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
        int i, min, max, start = 0, breakLine = 0, startNode = 1, subjectCounter = 0, propertyCounter = 0, cardinalityCounter = 0, samples = 1, cardinalityIndex, offset = 1;
        string prevNode = "", record = "", currentProperty = "", currentSubject = "", cardinality = "", tmp = "", valueSet = "", requestedMin, requestedMax, propertyForRecord, propertyValue, startNodeUri = "";
        bool firstTime = true, newSample = true, prevWasChecked = false, currIsChecked, countListItems, repeatedProperty = false;
        Result test;
        Range range;
        SparqlQuery query;
        Object results;

        //TurtleParser parser = new TurtleParser();
        //RdfXmlParser parser = new RdfXmlParser();
        try
        {
            //parser.Load(g, new StringReader(source.Text));
            StringParser.Parse(g, source.Text);
        }
        catch (RdfParseException parseEx)
        {
            RdfXmlParser parser2 = new RdfXmlParser();
            try
            {
                parser2.Load(g, new StringReader(source.Text));
                hideValidatorLink = true;
            }
            catch
            {
                RdfJsonParser parser3 = new RdfJsonParser();
                try
                {
                    parser3.Load(g, new StringReader(source.Text));
                    hideValidatorLink = true;
                }
                catch
                {
                    //This indicates a parser error e.g unexpected character, premature end of input, invalid syntax etc.
                    error = "Parsing error - please, check your input: " + parseEx.Message;
                    return;
                }
            }
        }
        catch (RdfException rdfEx)
        {
            //This represents a RDF error e.g. illegal triple for the given syntax, undefined namespace
            error = "RDF error - please, check your input: " + rdfEx.Message;
            return;
        }

        TripleStore store = new TripleStore();
        store.Add(g);

        //Create a dataset for our queries to operate over
        //We need to explicitly state our default graph or the unnamed graph is used
        //Alternatively you can set the second parameter to true to use the union of all graphs
        //as the default graph
        InMemoryDataset ds = new InMemoryDataset(store, true);

        //Get the Query processor
        processor = new LeviathanQueryProcessor(ds);

        /*SparqlQuery query = sparqlparser.ParseFromString("CONSTRUCT { ?s ?p ?o } WHERE {?s ?p ?o}");
        SparqlQuery query = sparqlparser.ParseFromString("SELECT * WHERE {?s ?p ?o}");
        string q = "PREFIX dct: <http://purl.org/dc/terms/> SELECT ?s (COUNT(?o) AS ?count) WHERE { ?s dct:isPartOf ?o . } GROUP BY ?s";*/

        string q = "PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> SELECT ?o (COUNT(?o) AS ?count) ?s WHERE { ?s rdf:type ?o FILTER(!isBlank(?s)) . } GROUP BY ?o ?s";
        query = sparqlparser.ParseFromString(q);
        results = processor.ProcessQuery(query);

        if (infoRequired.Value == "1" && nodeOptions.SelectedValue.Length == 0)
        {
            error = "Please, select class instance!";
        }
        if (results is SparqlResultSet)
        {
            //Print out the Results
            SparqlResultSet rset = (SparqlResultSet)results;
            if (rset.Count > 1 && rset[0].ToString().Split(',')[0] != rset[1].ToString().Split(',')[0])
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
                        if (prevNode != stringParts[0].Substring(5))
                        {
                            nodeOptions.Items.Add(new ListItem(stringParts[0].Substring(5), i.ToString()));
                            prevNode = stringParts[0].Substring(5);
                        }
                    }
                }
                else
                {
                    startNode = int.Parse(nodeOptions.SelectedValue);
                    i = 0;
                    foreach (SparqlResult result in rset)
                    {
                        i++;
                        if (startNode == i)
                        {
                            stringParts = result.ToString().Split(',');
                            startNodeUri = stringParts[1].Substring(5).Trim();
                            break;
                        }
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
            //error = "Incorrect RDF data format!";
            //return;
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
            //error = "Incorrect RDF data format!";
            //return;
        }

        record += Environment.NewLine + "my:Schema {";
        int k = -1, blankNodes = 0;
        foreach (Triple t in g.Triples)
        {
            if (t.Subject.NodeType == NodeType.Blank)
            {
                blankNodes++;
            }
        }
        g.NamespaceMap.AddNamespace("foaf", new Uri("http://xmlns.com/foaf/0.1/"));
        foreach (Triple t in g.Triples)
        {
            k++;
            if ((startNodeUri.Length != 0 && startNodeUri != t.Subject.ToString()) || t.Subject.NodeType == NodeType.Blank)
            {
                if (currentProperty.Length > 0)
                {
                    saveProperty(currentProperty, "1");
                    currentProperty = "";
                }
                continue;
            }
            if (currentSubject.Length == 0)
            {
                currentSubject = t.Subject.ToString();
            }
            else if (currentSubject != t.Subject.ToString())
            {
                if (prevWasChecked)
                {
                    saveProperty(currentProperty, valueSet);
                    currentProperty = tmp;
                    valueSet = "";
                    cardinalityCounter = 1;
                }
                else
                {
                    cardinalityCounter++;
                    saveProperty(currentProperty, cardinalityCounter.ToString());
                    cardinalityCounter = 0;
                }
                samples++;
                firstTime = true;
                newSample = true;
                currentSubject = t.Subject.ToString();
            }

            if (k == 0 || firstTime)
            {
                targetSubject = HttpUtility.UrlEncode(HttpUtility.HtmlEncode(getPropertysQName(t.Subject.ToString())));
            }
            cardinalityCounter++;
            propertyCounter++;
            tmp = "";
            currIsChecked = false;
            propertyForRecord = getPropertysQName(t.Predicate.ToString());
            repeatedProperty = (propertyRepeatTimes(t.Subject.ToString(), t.Predicate.ToString()) > offset);
            propertyValue = getPropertysQName(t.Object.ToString());

            currIsChecked = isPropertyChecked(propertyForRecord);
            if (!currIsChecked)
            {
                test = hasLanguageTag(propertyValue);
                if (t.Object.NodeType == NodeType.Literal && test.Answer == true)
                {
                    tmp = propertyForRecord + " [" + test.Contents.TrimStart('"') + "]";
                }
                else if (t.Object.NodeType == NodeType.Literal && (test = hasStringProperty(t.Object.ToString())).Answer == true)
                {
                    tmp = propertyForRecord + " xsd:string";
                }
                else if (t.Object.NodeType == NodeType.Literal && hasDateProperty(t.Object.ToString()))
                {
                    tmp = propertyForRecord + " xsd:date";
                }
                else if (t.Object.NodeType == NodeType.Literal && hasIntegerProperty(t.Object.ToString()))
                {
                    tmp = propertyForRecord + " xsd:integer";
                    hasNumericValues = true;
                }
                else if (t.Object.NodeType == NodeType.Literal && hasDecimalProperty(t.Object.ToString()))
                {
                    tmp = propertyForRecord + " xsd:decimal";
                    hasNumericValues = true;
                }
                else if (t.Object.NodeType == NodeType.Uri && (test = hasIRIWithPrefix(propertyValue)).Answer == true)
                {
                        if (propertyForRecord == "rdf:type")
                        {
                            propertyForRecord = "a";
                        }
                        tmp = propertyForRecord + ((propertyForRecord == "a") ? " [" + test.Contents + "]" : " IRI");
                }
                else if (t.Object.NodeType == NodeType.Uri)
                {
                    tmp = propertyForRecord + " IRI";
                }
                else if (t.Object.NodeType == NodeType.Blank)
                {
                    /*if ((test = hasBlankNode(t.Subject.ToString(), t.Predicate.ToString(), t.Object.ToString())).Answer == true)
                    {
                        tmp = propertyForRecord + " " + test.Contents;
                    }
                    else
                    {
                        tmp = propertyForRecord + " BNode";
                    }*/
                    tmp = propertyForRecord + " BNode";
                }

                if (tmp.Length == 0)
                {
                    error = "Unrecognized property's value! (" + lastTestedPropertyValue + ")";
                    break;
                }
            }
            else
            {
                tmp = propertyForRecord;
            }

            if (k == 0 || newSample)
            {
                currentProperty = tmp;
                newSample = false;
                if (currIsChecked)
                {
                    valueSet = propertyValue;
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
                    valueSet = propertyValue;
                    prevWasChecked = true;
                    cardinalityCounter = 0;
                }
                else if (currentProperty == tmp)
                {
                    valueSet += " " + propertyValue;
                }
                else
                {
                    saveProperty(currentProperty, valueSet);
                    currentProperty = tmp;
                    valueSet = propertyValue;
                }
                if (tmp.Length == 0)
                {
                    break;
                }
            }
            if ((!currIsChecked && currentProperty != tmp) || (k == g.Triples.Count - 1 - blankNodes && !repeatedProperty))
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
                if (currentProperty == tmp && k == g.Triples.Count - 1 - blankNodes && !repeatedProperty && !firstTime)
                {
                    cardinalityCounter++;
                }
                if (cardinalityCounter > 1)
                {
                    cardinality = "{" + cardinalityCounter + "}";
                }
                if (currentProperty.Length > 0 && !prevWasChecked)
                {
                    saveProperty(currentProperty, cardinalityCounter.ToString());
                }
                if (currentProperty != tmp && k == g.Triples.Count - 1 - blankNodes && !repeatedProperty)
                {
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
            }
            else
            {
                offset = 1;
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
        MatchCollection matches = quoteRegex.Matches('"' + property + '"');
        lastTestedPropertyValue = property;
        if (matches.Count > 0)
        {
            string tmp = "";
            foreach (Match match in matches)
            {
                if (
                    property.IndexOf("^^http://www.w3.org/2001/XMLSchema#date") != -1
                    ||
                    property.IndexOf("^^http://www.w3.org/2001/XMLSchema#integer") != -1
                    ||
                    property.IndexOf("^^http://www.w3.org/2001/XMLSchema#decimal") != -1
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
        lastTestedPropertyValue = property;
        return (property.IndexOf("^^http://www.w3.org/2001/XMLSchema#integer") != -1);
    }

    private bool hasDecimalProperty(string property)
    {
        lastTestedPropertyValue = property;
        return (property.IndexOf("^^http://www.w3.org/2001/XMLSchema#decimal") != -1);
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
        lastTestedPropertyValue = property;
        return (property.IndexOf("^^http://www.w3.org/2001/XMLSchema#date") != -1);
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
        Regex tagRegex = new Regex(@"@[a-z]{2}(-[a-zA-Z]{2})?""?$");
        MatchCollection matches = tagRegex.Matches(property);
        if (matches.Count > 0)
        {
            string tmp = "";
            foreach (Match match in matches)
            {
                tmp += match.Value.TrimEnd('"') + "~";
            }
            return new Result(true, tmp);
        }
        return new Result(false, "N/A");
    }

    private Result hasBlankNode(string subject, string predicate, string objectName)
    {
        string q = "PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> SELECT ?o WHERE { <" + subject + "> <" + predicate + "> " + objectName + " . " + objectName + " rdf:type ?o . }";
        SparqlQuery query = sparqlparser.ParseFromString(q);
        Object results = processor.ProcessQuery(query);

        if (results is SparqlResultSet)
        {
            SparqlResultSet rset = (SparqlResultSet)results;
            foreach (SparqlResult result in rset)
            {
                return new Result(true, getPropertysQName(result.ToString().Substring(5)));
            }
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
                if (value.Length == 0)
                {
                    try
                    {
                        value = "<" + g.NamespaceMap.GetNamespaceUri(stringMatch.TrimEnd(':')).ToString() + ">";
                    }
                    catch(RdfException ex)
                    {
                        value = "";
                    }
                }
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

    private string[] splitAndClear(string text, char splitter = ' ')
    {
        string trimmedString = text.Trim(removeSymbols);
        string[] stringParts = trimmedString.Split(splitter);
        int i = 0;
        foreach (string tmp in stringParts)
        {
            stringParts[i] = tmp.Trim();
            i++;
        }
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
        MatchCollection matches, tagMatches;
        Regex tagRegex = new Regex(@"""(.+)""@[a-z]{2}(-[a-zA-Z]{2})?""?$");
        List<string> newList = new List<string>();
        char separator;

        foreach (string item in list)
        {
            matches = quoteRegex.Matches(item);
            tagMatches = tagRegex.Matches(item);
            if (tagMatches.Count > 0)
            {
                foreach (Match match in tagMatches)
                {
                    newList.Add(match.Value);
                }
            }
            else if (matches.Count > 0 && hasLanguageTag(item).Answer == false && item.IndexOf("^^xsd:date") == -1 && item.IndexOf("^^xsd:integer") == -1 && item.IndexOf("^^xsd:decimal") == -1)
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

    private string getPropertysQName(string uri)
    {
        string qName;
        Result test;
        if (g.NamespaceMap.ReduceToQName(uri, out qName))
        {
            return (qName == "rdf:type")? "a" : qName;
        }
        if ((test = hasLanguageTag(uri)).Answer == true)
        {
            return '"' + uri.Substring(0, uri.Length - test.Contents.Length) + '"' + uri.Substring(uri.Length - test.Contents.Length);
        }
        if (uri.IndexOf("://") == -1 && hasStringProperty(uri).Answer == true)
        {
            return '"' + uri + '"';
        }
        if (hasIntegerProperty(uri))
        {
            return uri.Replace("^^http://www.w3.org/2001/XMLSchema#integer", "");
        }
        if (hasDecimalProperty(uri))
        {
            return uri.Replace("^^http://www.w3.org/2001/XMLSchema#decimal", "");
        }
        if (hasDateProperty(uri))
        {
            return '"' + uri.Replace("^^http://www.w3.org/2001/XMLSchema#date", "") + "\"^^xsd:date";
        }
        return (uri.IndexOf("://") != -1 && uri.IndexOf("^^") == -1) ? "<" + uri + ">" : uri;
    }

    private int propertyRepeatTimes(string subject, string predicate)
    {
        string q = "SELECT COUNT(?o) AS ?count WHERE { <" + subject + "> <" + predicate + "> ?o . } GROUP BY ?o";
        SparqlQuery query = sparqlparser.ParseFromString(q);
        Object results = processor.ProcessQuery(query);

        if (results is SparqlResultSet)
        {
            SparqlResultSet rset = (SparqlResultSet)results;
            return rset.Count;
        }
        return 0;
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