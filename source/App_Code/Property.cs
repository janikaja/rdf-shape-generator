using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for Property
/// </summary>
public class Property
{
    private int index;
    private string name;
    private bool is_checked;

    public Property(int index, string name, bool is_checked)
    {
        Index = index;
        Name = name;
        Is_checked = is_checked;
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
}