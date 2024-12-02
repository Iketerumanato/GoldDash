using UnityEngine;

public class NamedArrayAttribute : PropertyAttribute
{
    public readonly string[] elementnames;
    public NamedArrayAttribute(string[] names) { this.elementnames = names; }
}