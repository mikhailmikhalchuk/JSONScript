using JSONScript.Runtime;

namespace JSONScript.VM.Graphics
{
    public interface IGraphicsBackend
    {
        void Init(int width, int height, string title);
        void DrawRect(float x, float y, float w, float h, float r, float g, float b);
        void BeginFrame();
        void EndFrame();
        void RunLoop();
        void SetEventHandler(Action<string, Value[]> handler);
        nint GetLayerPtr();
        nint GetDevicePtr();
    }
}