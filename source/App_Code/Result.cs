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

    public Result(bool answer, string contents)
    {
        Answer = answer;
        Contents = contents;
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
}