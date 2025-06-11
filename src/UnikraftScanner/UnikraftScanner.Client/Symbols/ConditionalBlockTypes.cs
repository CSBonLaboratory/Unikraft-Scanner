namespace UnikraftScanner.Client.Symbols;

public enum ConditionalBlockTypes
{
    IF = 0,
    IFDEF = 1,
    ELIF = 2,
    ELSE = 3,
    IFNDEF = 4,
    ENDIF = 5,

    // used in CountLinesOfCodeInSourceFile to represente the whole source file as a compilation block for counting convenience
    ROOT = 6

}