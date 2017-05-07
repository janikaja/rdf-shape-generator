using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for Range
/// </summary>
public class Range
{
    private string min_type, max_type, min_value, max_value;
    private bool is_empty;

    public Range()
    {
        is_empty = true;
    }

    public Range(string min_type, string min_value, string max_type, string max_value)
    {
        Min_type = min_type;
        Min_value = min_value;
        Max_type = max_type;
        Max_value = max_value;
        Is_empty = false;
    }

    public string Min_type
    {
        get
        {
            return min_type;
        }

        set
        {
            min_type = value;
        }
    }

    public string Min_value
    {
        get
        {
            return min_value;
        }

        set
        {
            min_value = value;
        }
    }

    public string Max_type
    {
        get
        {
            return max_type;
        }

        set
        {
            max_type = value;
        }
    }

    public string Max_value
    {
        get
        {
            return max_value;
        }

        set
        {
            max_value = value;
        }
    }

    public bool Is_empty
    {
        get
        {
            return is_empty;
        }

        set
        {
            is_empty = value;
        }
    }

    public string getStandardValueMin()
    {
        return min_value.Replace(',', '.');
    }

    public string getStandardValueMax()
    {
        return max_value.Replace(',', '.');
    }
}