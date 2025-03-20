using System;


public class IntArgs : EventArgs
{
    public int value;

    public IntArgs(int v)
    {
        this.value = v;
    }
}