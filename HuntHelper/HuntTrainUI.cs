using System;
using HuntHelper.Managers.Hunts;

namespace HuntHelper;

public class HuntTrainUI : IDisposable
{
    private readonly HuntManager _huntManager;

    public HuntTrainUI(HuntManager huntManager)
    {
        _huntManager = huntManager;

    }

    public void Draw()
    {

    }

    public void Dispose()
    {

    }
    
}