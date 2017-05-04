using System;
using System.Collections.Generic;
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
using VDS.RDF.Writing.Formatting;

public partial class _Default : Page
{
    public string result = "", error = "";
    public bool additionalInfoRequired = false;
    public List<Property> foundProperties = new List<Property>();
    private Dictionary<string, string> prefixDictionary = new Dictionary<string, string>();
    private Dictionary<string, List<int>> propertyDictionary = new Dictionary<string, List<int>>();
    private string lastTestedPropertyValue;
    private char[] removeSymbols = { ';', '.', ' ', '\t' };

    protected void Page_Load(object sender, EventArgs e)
    {
        if (SampleList.SelectedIndex < 1)
        {
            SampleList.Items.Clear();
            SampleList.Items.Add(new ListItem("", "0"));
            for (int j = 1; j < 4; j++)
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
        int i, min, max, start = 0, breakLine = 0, startNode = 1, counter = 0, samples = 1;
        string property, record = "", currentProperty = "", cardinality = "", tmp = "", valueSet = "";
        bool firstTime = true, newSample = true, isChecked = false;
        Result test;

        Graph g = new Graph();
        //TurtleParser parser = new TurtleParser();
        try
        {
            //parser.Load(g, new StringReader(source.Text));
            StringParser.Parse(g, source.Text);
        }
        catch (RdfParseException parseEx)
        {
            //This indicates a parser error e.g unexpected character, premature end of input, invalid syntax etc.
            error = "Parser Error: ";
            error += parseEx.Message;
        }
        catch (RdfException rdfEx)
        {
            //This represents a RDF error e.g. illegal triple for the given syntax, undefined namespace
            error = "RDF Error: ";
            error += rdfEx.Message;
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
                counter++;
                if (counter == startNode)
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
            record += Environment.NewLine + "my:Shape {";
            counter = 0;
            for (int k = i + 2; k < sourceLines.Length; k++)
            {
                property = sourceLines[k].Trim(removeSymbols);
                propertyParts = splitAndClear(sourceLines[k]);
                if (propertyParts.Length == 1)
                {
                    continue;
                }
                counter++;
                tmp = "";

                if (property.Length > 0)
                {
                    if (RadioButtonList1.SelectedValue == "datatypes")
                    {
                        test = hasStringProperty(property);
                        if (test.Answer == true)
                        {
                            tmp = propertyParts[0] + " xsd:string";
                        }
                        else if (hasDateProperty(propertyParts[1]))
                        {
                            tmp = propertyParts[0] + " xsd:date";
                        }
                        else if (hasIntegerProperty(propertyParts[1]))
                        {
                            tmp = propertyParts[0] + " xsd:integer";
                        }
                        else if (hasFloatProperty(propertyParts[1]))
                        {
                            tmp = propertyParts[0] + " xsd:float";
                        }
                        else if (hasIriProperty(property))
                        {
                            tmp = propertyParts[0] + " IRI";
                        }
                        else
                        {
                            test = hasValueSet(propertyParts[1]);
                            if (test.Answer == true)
                            {
                                tmp = propertyParts[0] + " [" + test.Contents + "]";
                            }
                            else
                            {
                                test = hasLanguageTag(property);
                                if (test.Answer == true)
                                {
                                    tmp = propertyParts[0] + " [" + test.Contents.TrimStart('"') + "]";
                                }
                            }
                        }

                        if (tmp.Length == 0)
                        {
                            error = "Unrecognized RDF property value! (" + lastTestedPropertyValue + ")";
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
                    if (RadioButtonList1.SelectedValue == "values")
                    {
                        valueSet = property.Substring(tmp.Length + 1);
                    }
                }
                else if (RadioButtonList1.SelectedValue == "values")
                {
                    if (currentProperty == tmp)
                    {
                        valueSet += " " + property.Substring(tmp.Length + 1);
                    }
                    else
                    {
                        cardinality = " [" + valueSet + "]";
                        if (property.Length > 0)
                        {
                            valueSet = property.Substring(tmp.Length + 1);
                        }
                        record += Environment.NewLine + "\t" + currentProperty + cardinality + ";";
                        currentProperty = tmp;
                    }
                    if (tmp.Length == 0)
                    {
                        break;
                    }
                }
                if (RadioButtonList1.SelectedValue == "datatypes" && (currentProperty != tmp || k == sourceLines.Length - 1))
                {
                    if (firstTime && counter > 1)
                    {
                        firstTime = false;
                        counter--;
                    }
                    if (currentProperty == tmp && k == sourceLines.Length - 1)
                    {
                        counter++;
                    }
                    if (counter > 1)
                    {
                        cardinality = "{" + counter + "}";
                    }
                    //record += "\t" + currentProperty + cardinality + ";" + Environment.NewLine;
                    saveProperty(currentProperty, counter);
                    if (tmp.Length == 0)
                    {
                        if (nodeOptions.SelectedValue.Length == 0 && k + 1 <= sourceLines.Length - 1 && sourceLines[k + 1].Trim(removeSymbols).Length > 0)
                        {
                            samples++;
                            currentProperty = "";
                            counter = 0;
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
                    if (currentProperty != tmp && k == sourceLines.Length - 1)
                    {
                        //record += "\t" + tmp + Environment.NewLine;
                        saveProperty(currentProperty, 1);
                    }
                    currentProperty = tmp;
                    counter = 0;
                    cardinality = "";
                }
            }
            i = 0;
            foreach (string key in propertyDictionary.Keys)
            {
                if (propertyDictionary[key].Count < samples)
                {
                    min = 0;
                }
                else
                {
                    min = propertyDictionary[key].Min();
                }
                max = propertyDictionary[key].Max();
                if (min == 0 && max == 1)
                {
                    cardinality = "?";
                }
                else if (min == 0 && max > 1)
                {
                    cardinality = "*";
                }
                else if (min == 1 && max > 1)
                {
                    cardinality = "+";
                }
                else if (min == max && min != 1 && key[key.Length - 1] != ']')
                {
                    cardinality = "{" + min + "}";
                }
                else if (min < max)
                {
                    cardinality = "{" + min + "," + max + "}";
                }
                else
                {
                    cardinality = "";
                }
                record += Environment.NewLine + "\t" + key + cardinality + ";";
                isChecked = (Page.IsPostBack && Request.Form["Checkbox" + i] == "1");
                foundProperties.Add(new Property(i, key + cardinality, isChecked));
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
        Regex quoteRegex = new Regex(@"""[^""\\]*(?:\\.[^""\\]*)*""$");
        MatchCollection matches = quoteRegex.Matches(property);
        lastTestedPropertyValue = property;
        if (matches.Count > 0)
        {
            string tmp = "";
            foreach (Match match in matches)
            {
                if (hasDateProperty(match.ToString().Trim('"')))
                {
                    return new Result(false, "N/A");
                }
                tmp += match.Value;
            }
            return new Result(true, tmp);
        }
        return new Result(false, "N/A");
    }

    private bool hasIntegerProperty(string property)
    {
        Regex integerRegex = new Regex(@"^\d+$");
        MatchCollection matches = integerRegex.Matches(property);
        lastTestedPropertyValue = property;
        return (matches.Count > 0);
    }

    private bool hasFloatProperty(string property)
    {
        Regex floatRegex = new Regex(@"^\d*\.\d+$");
        MatchCollection matches = floatRegex.Matches(property);
        lastTestedPropertyValue = property;
        return (matches.Count > 0);
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
        Regex dateRegex = new Regex(@"(([1-9][0-9]|[1-9])\d{2})-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])");
        MatchCollection matches = dateRegex.Matches(property);
        lastTestedPropertyValue = property;
        return (matches.Count > 0);
    }

    private Result hasValueSet(string property)
    {
        Regex valueSetRegex = new Regex(@"^[a-z]+:[a-zA-Z]+$");
        MatchCollection matches = valueSetRegex.Matches(property);
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
                tmp += match.Value;
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

    private void saveProperty(string key, int value)
    {
        if (!propertyDictionary.ContainsKey(key))
        {
            List<int> list = new List<int>();
            list.Add(value);
            propertyDictionary[key] = list;
        }
        else
        {
            propertyDictionary[key].Add(value);
        }
    }

    protected void SampleList_SelectedIndexChanged(object sender, EventArgs e)
    {
        resultText.Text = "";
        source.Text = Sample.getSample(SampleList.SelectedIndex);
    }
}