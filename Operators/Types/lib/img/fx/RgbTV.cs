using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5972a57b_73cd_49b2_8b24_96636a4c294b
{
    public class RgbTV : Instance<RgbTV>
    {
        [Output(Guid = "22eac013-881d-486a-8041-5cae32b8dca1")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "2dbfdd5d-8b4b-447c-bd19-326d46657ea1")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "402ec136-4727-41b7-8bfb-344d1ed83a48")]
        public readonly InputSlot<float> Visibility = new InputSlot<float>();

        [Input(Guid = "38529a44-4622-4c87-886e-72f4400ec468")]
        public readonly InputSlot<float> PatternAmount = new InputSlot<float>();

        [Input(Guid = "3ef09b89-b3e8-432b-bed3-f7c9033acefa")]
        public readonly InputSlot<float> ImageBrightess = new InputSlot<float>();

        [Input(Guid = "50cab12f-535c-4651-98d2-5c8b3c18cc81")]
        public readonly InputSlot<float> BlackLevel = new InputSlot<float>();

        [Input(Guid = "88258a10-d5e4-4a4e-80b3-eacaebe75abf")]
        public readonly InputSlot<float> ImageContrast = new InputSlot<float>();

        [Input(Guid = "fc4e5ca3-edb3-4ebc-8210-b339d7914741")]
        public readonly InputSlot<float> BlurImage = new InputSlot<float>();

        [Input(Guid = "e29b1dad-d3ef-405f-99c0-4552caedaf7c")]
        public readonly InputSlot<float> GlowIntensity = new InputSlot<float>();

        [Input(Guid = "51f05bd9-6f7e-4774-a05d-acaa1b8cba35")]
        public readonly InputSlot<float> GlowBlur = new InputSlot<float>();

        [Input(Guid = "866493b5-50ec-4587-861f-87d5d3a698e5")]
        public readonly InputSlot<float> PatternSize = new InputSlot<float>();

        [Input(Guid = "be5ee091-ceea-4b69-8e81-427c8696cdeb")]
        public readonly InputSlot<float> ShiftColumns = new InputSlot<float>();

        [Input(Guid = "f58da234-b925-40ca-b449-1ac882bd2e96")]
        public readonly InputSlot<float> Gaps = new InputSlot<float>();

        [Input(Guid = "5f536f1f-49da-435d-a02b-002a3673c240")]
        public readonly InputSlot<System.Numerics.Vector2> PatternBlur = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "f76c6202-34dc-4c10-adab-c10cb7665fed")]
        public readonly InputSlot<float> GlitchAmount = new InputSlot<float>();

        [Input(Guid = "5cb6a27b-db0d-4d85-83aa-87316ba6f5a0")]
        public readonly InputSlot<float> GlitchFlicker = new InputSlot<float>();

        [Input(Guid = "895e4da6-fd6e-436f-b414-14c42cbb3259")]
        public readonly InputSlot<float> GlitchTimeOverride = new InputSlot<float>();

        [Input(Guid = "026b6679-10e9-41e5-84b0-2c4b3f86b155")]
        public readonly InputSlot<float> GlitchDistort = new InputSlot<float>();

        [Input(Guid = "c71c9adc-eae6-4acb-b1f0-4eea873a5bce")]
        public readonly InputSlot<float> ShadeDistortion = new InputSlot<float>();

        [Input(Guid = "749a9cb1-9dd7-43c7-af91-d5624e26423c")]
        public readonly InputSlot<float> NoiseForDistortion = new InputSlot<float>();

        [Input(Guid = "a13b757c-62ed-478b-b0fe-70cceb43586e")]
        public readonly InputSlot<float> Noise = new InputSlot<float>();

        [Input(Guid = "795f9829-7dbf-4e07-82e5-d03eead8c527")]
        public readonly InputSlot<float> NoiseSpeed = new InputSlot<float>();

        [Input(Guid = "69b4b2d0-cede-4479-a976-855b69107b8d")]
        public readonly InputSlot<float> NoiseExponent = new InputSlot<float>();

        [Input(Guid = "06781f30-4be8-4eb6-95fd-7e8b16081a64")]
        public readonly InputSlot<float> NoiseColorize = new InputSlot<float>();

        [Input(Guid = "e3e3f393-0c43-4c71-b134-adc094ca2965")]
        public readonly InputSlot<float> Buldge = new InputSlot<float>();

        [Input(Guid = "a24de125-eb69-4c44-afa6-69dfdbf16087")]
        public readonly InputSlot<float> Vignette = new InputSlot<float>();

        [Input(Guid = "e3f06019-97a4-44b3-b9b4-36d07cbd53ac")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();
    }
}

