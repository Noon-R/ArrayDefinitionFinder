namespace ArrayFinder.Core.Models;

public enum DeclarationKind
{
    Field,
    Property,
    Method,
    Constructor,
    Local,
    Parameter,
    Type,   // class / struct / interface / enum / record
    Event,
}
