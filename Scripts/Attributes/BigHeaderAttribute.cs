using System;
using UnityEngine;

public class BigHeaderAttribute : PropertyAttribute
{
    public string _Text {
        get { return mText; }
    }

    private string mText = String.Empty;

    public BigHeaderAttribute(string text)
    {
        mText = text;
    }
}
