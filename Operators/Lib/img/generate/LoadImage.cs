namespace lib.img.generate
{
    [Guid("0b3436db-e283-436e-ba85-2f3a1de76a9d")]
    public class LoadImage : Instance<LoadImage>, IDescriptiveFilename
    {
        [Output(Guid = "{E0C4FEDD-5C2F-46C8-B67D-5667435FB037}")]
        public readonly Slot<Texture2D> Texture = new();

        [Output(Guid = "{A4A46C04-FF03-48CE-83C9-0C0BAA0F72E7}")]
        public readonly Slot<ShaderResourceView> ShaderResourceView = new();

        public LoadImage()
        {
            _textureResource = ResourceManager.CreateTextureResource(Path);
            _textureResource.AddDependentSlots(Texture);
            Texture.UpdateAction = UpdateTexture;
            ShaderResourceView.UpdateAction = UpdateTexture;
        }

        private void UpdateTexture(EvaluationContext context)
        {
            Texture.Value = _textureResource.GetValue(context);
            Texture.DirtyFlag.Clear();

            var currentSrv = ShaderResourceView.Value;
            if (Texture.Value == null)
            {
                currentSrv?.Dispose();
                ShaderResourceView.Value = null;
                ShaderResourceView.DirtyFlag.Clear();
                return;
            }
            
            Texture.Value.CreateShaderResourceView(ref currentSrv, "");

            try
            {
                ResourceManager.Device.ImmediateContext.GenerateMips(currentSrv);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to generate mipmaps for texture {Path.Value}:" + exception);
            }

            ShaderResourceView.Value = currentSrv;
            ShaderResourceView.DirtyFlag.Clear();
        }

        [Input(Guid = "{76CC3811-4AE0-48B2-A119-890DB5A4EEB2}")]
        public readonly InputSlot<string> Path = new();

        public IEnumerable<string> FileFilter => FileFilters;
        private static readonly string[] FileFilters = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.tga", "*.dds", "*.gif"];
        public InputSlot<string> SourcePathSlot => Path;

        private readonly Resource<Texture2D> _textureResource;
    }
}