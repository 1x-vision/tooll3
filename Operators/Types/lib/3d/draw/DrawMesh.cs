using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Rendering.Material;

namespace T3.Operators.Types.Id_a3c5471e_079b_4d4b_886a_ec02d6428ff6
{
    public class DrawMesh : Instance<DrawMesh>, ICustomDropdownHolder, ICompoundWithUpdate
    {
        [Output(Guid = "53b3fdca-9d5e-4808-a02f-4aa743cd8456")]
        public readonly Slot<Command> Output = new();
        
        
        public DrawMesh()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (context.Materials != null)
            {
                _pbrMaterials.Clear();
                _pbrMaterials.AddRange(context.Materials);
            }

            var previousMaterial = context.PbrMaterial;
            
            var materialId = UseMaterialId.GetValue(context);
            if (!string.IsNullOrEmpty(materialId))
            {
                foreach(var m in context.Materials)
                {
                    if (m.Name != materialId)
                        continue;
                    
                    context.PbrMaterial = m;
                    break;

                }
            }
            
            // Inner update
            Output.ConnectedUpdate(context);
            context.PbrMaterial = previousMaterial;
        }

        #region custom material dropdown
        string ICustomDropdownHolder.GetValueForInput(Guid inputId)
        {
            return inputId != UseMaterialId.Input.InputDefinition.Id 
                       ? "Undefined input" 
                       : UseMaterialId.TypedInputValue.Value;
        }

        IEnumerable<string> ICustomDropdownHolder.GetOptionsForInput(Guid inputId)
        {
            yield return "Default";
            
            if(_pbrMaterials == null)
                yield break;

            foreach (var m in _pbrMaterials)
            {
                yield return string.IsNullOrEmpty(m.Name) ? "undefined" : m.Name;
            }
        }

        void ICustomDropdownHolder.HandleResultForInput(Guid inputId, string result)
        {
            if (inputId != UseMaterialId.Input.InputDefinition.Id)
                return;
            
            UseMaterialId.SetTypedInputValue(result);
        }

        private readonly List<PbrMaterial> _pbrMaterials = new(8);
        #endregion
        

        [Input(Guid = "97429e1f-3f30-4789-89a6-8e930e356ee6")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new InputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "8c9dee45-d165-48c8-b8dd-b7f47e77fd00")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "4748d9ab-58a4-41d7-a2ee-6f7dfed86211")]
        public readonly InputSlot<float> AlphaCutOff = new InputSlot<float>();

        [Input(Guid = "9c17fa15-35f1-49d4-802f-a3a796cad96a", MappedType = typeof(SharedEnums.BlendModes))]
        public readonly InputSlot<int> BlendMode = new InputSlot<int>();

        [Input(Guid = "2c4b5f3a-e9ec-432e-b1ae-6d999ae44f1b", MappedType = typeof(FillMode))]
        public readonly InputSlot<int> FillMode = new InputSlot<int>();

        [Input(Guid = "9e957f4a-6502-4905-8d97-331f8b54097c")]
        public readonly InputSlot<SharpDX.Direct3D11.CullMode> Culling = new InputSlot<SharpDX.Direct3D11.CullMode>();

        [Input(Guid = "b50b3fc7-35e1-421d-be0a-b3008a54c33c")]
        public readonly InputSlot<bool> EnableZTest = new InputSlot<bool>();

        [Input(Guid = "dfad3400-885a-4f83-8c39-ec6520f4e2aa")]
        public readonly InputSlot<bool> EnableZWrite = new InputSlot<bool>();

        [Input(Guid = "155c2396-0e05-4437-8171-288048b1158a")]
        public readonly InputSlot<SharpDX.Direct3D11.Filter> Filter = new InputSlot<SharpDX.Direct3D11.Filter>();

        [Input(Guid = "d1db33ea-1739-4323-9105-7b236a0e240f")]
        public readonly InputSlot<SharpDX.Direct3D11.TextureAddressMode> WrapMode = new();
        
        [Input(Guid = "D7BD3003-8589-4537-92E8-E95C5EB2BFAB")]
        public readonly InputSlot<string> UseMaterialId = new ();


    }
}

