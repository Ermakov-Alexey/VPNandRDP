using System;

namespace VaR;

public static class Versions
{
    // https://en.wikipedia.org/wiki/Remote_Desktop_Protocol
    public static readonly Version Rdc60 = new(6, 0, 6000);
    public static readonly Version Rdc61 = new(6, 0, 6001);
    public static readonly Version Rdc70 = new(6, 1, 7600);
    public static readonly Version Rdc80 = new(6, 2, 9200);
    public static readonly Version Rdc81 = new(6, 3, 9600);
    public static readonly Version Rdc100 = new(10, 0, 0);
}