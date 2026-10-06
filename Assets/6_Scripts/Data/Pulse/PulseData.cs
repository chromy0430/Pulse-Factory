using System;

[Serializable]
public struct PulseData
{
    public int Id;
    public float Power;

    public PulseData(int id, float power)
    {
        Id = id;
        Power = power;
    }
}