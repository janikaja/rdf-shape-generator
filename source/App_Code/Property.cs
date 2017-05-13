using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;

/// <summary>
/// Summary description for Property
/// </summary>
public class Property
{
    private int index, cardinality_index;
    private string name, min, max;
    private bool is_checked;
    private Range range;

    public Property(int index, string name, bool is_checked, string min, string max, int cardinality_index, Range range)
    {
        Index = index;
        Name = name;
        Is_checked = is_checked;
        Min = min;
        Max = max;
        Cardinality_index = cardinality_index;
        Range = range;
    }

    public int Index
    {
        get
        {
            return index;
        }

        set
        {
            index = value;
        }
    }

    public int Cardinality_index
    {
        get
        {
            return cardinality_index;
        }

        set
        {
            cardinality_index = value;
        }
    }

    public string Name
    {
        get
        {
            return name;
        }

        set
        {
            name = value;
        }
    }

    public string Min
    {
        get
        {
            return min;
        }

        set
        {
            min = value;
        }
    }

    public string Max
    {
        get
        {
            return max;
        }

        set
        {
            max = value;
        }
    }

    public bool Is_checked
    {
        get
        {
            return is_checked;
        }

        set
        {
            is_checked = value;
        }
    }

    public Range Range
    {
        get
        {
            return range;
        }

        set
        {
            range = value;
        }
    }

    public bool hasValueSet()
    {
        Regex valueSetRegex = new Regex(@"\[(.+)\]");
        MatchCollection matches = valueSetRegex.Matches(name);
        return (matches.Count > 0);
    }

    public bool hasIntegerProperty()
    {
        string[] nameParts = name.Split(' ');
        return (nameParts.Length > 1 && nameParts[1].Length >= 11 && nameParts[1].Substring(0, 11) == "xsd:integer");
    }

    public static bool hasIntegerProperty2(string name)
    {
        string[] nameParts = name.Split(' ');
        return (nameParts.Length > 1 && nameParts[1].Length >= 11 && nameParts[1].Substring(0, 11) == "xsd:integer");
    }

    public bool hasDecimalProperty()
    {
        string[] nameParts = name.Split(' ');
        return (nameParts.Length > 1 && nameParts[1].Length >= 11 && nameParts[1].Substring(0, 11) == "xsd:decimal");
    }

    public static bool hasDecimalProperty2(string name)
    {
        string[] nameParts = name.Split(' ');
        return (nameParts.Length > 1 && nameParts[1].Length >= 11 && nameParts[1].Substring(0, 11) == "xsd:decimal");
    }
}