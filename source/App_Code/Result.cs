using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for Result
/// </summary>
public class Result
{
    private bool answer;
    private string contents;
    private int cardinality;

    public Result(bool answer, string contents, int cardinality = 1)
    {
        Answer = answer;
        Contents = contents;
        Cardinality = cardinality;
    }

    public bool Answer
    {
        get
        {
            return answer;
        }

        set
        {
            answer = value;
        }
    }

    public string Contents
    {
        get
        {
            return contents;
        }

        set
        {
            contents = value;
        }
    }

    public int Cardinality
    {
        get
        {
            return cardinality;
        }

        set
        {
            cardinality = value;
        }
    }
}