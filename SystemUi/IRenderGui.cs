namespace T3.SystemUi;

public interface IRenderGui<in TRenderDevice, in TDrawData> : IDisposable
{
    public void RenderDrawData(TDrawData drawData);
    public void Initialize(TRenderDevice device, int width, int height);
    public bool CreateDeviceObjects();
}