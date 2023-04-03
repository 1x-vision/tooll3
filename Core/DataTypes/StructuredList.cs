﻿using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;
using T3.Core.Logging;
using T3.Core.Resource;

namespace T3.Core.DataTypes
{
    public abstract class StructuredList
    {
        public StructuredList(Type type)
        {
            Type = type;
        }

        
        // public StructuredList(JsonTextReader reader, Type type)
        // {
        //     Type = type;
        //     var testList = Read(reader);
        // }

        public Type Type { get; }
        public abstract object Elements { get; }
        public abstract object this[int i] { get; set; }
        public abstract int NumElements { get; }
        public abstract int ElementSizeInBytes { get; }
        public abstract int TotalSizeInBytes { get; }
        public abstract void WriteToStream(DataStream stream);
        public abstract StructuredList Clone();

        public abstract StructuredList Join(params StructuredList[] other);

        // public abstract StructuredList Filter(Func<object, bool> filter);
        public abstract void Insert(int index, object obj);
        public abstract void Remove(int index);
        public abstract void SetLength(int length);
        public abstract void Write(JsonTextWriter writerNotUsed);

        public abstract StructuredList Read(JsonTextReader reader);
        

        protected static readonly JsonLoadSettings LoadSettings = new()
                                                                    {
                                                                        CommentHandling = CommentHandling.Ignore,
                                                                        DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                                                                        LineInfoHandling = LineInfoHandling.Ignore
                                                                    };
    }

    public class StructuredList<T> : StructuredList where T : struct
    {
        public StructuredList(T[] array) : base(typeof(T))
        {
            TypedElements = array;
        }
        
        public StructuredList(int count) : base(typeof(T))
        {
            TypedElements = new T[count];
        }

        public StructuredList() : base(typeof(T))
        {
            //TypedElements = new T[count];
        }


        public T[] TypedElements { get; private set; }

        public override object Elements => TypedElements;

        public override object this[int i]
        {
            get
            {
                if (i >= 0 && i < NumElements)
                {
                    return TypedElements[i];
                }

                Log.Debug($"StructuredList.GetElement: Tried to use index {i}, but length is {TypedElements.Length}.");
                return null;
            }
            set => TypedElements[i] = (T)value;
        }

        public override int NumElements => TypedElements.Length;
        public override int ElementSizeInBytes => Marshal.SizeOf<T>();
        public override int TotalSizeInBytes => NumElements * ElementSizeInBytes;

        public override void WriteToStream(DataStream stream)
        {
            if (NumElements > 0)
                stream.WriteRange(TypedElements);
        }

        public override StructuredList Clone()
        {
            var clone = new StructuredList<T>(NumElements);
            Array.Copy(TypedElements, clone.TypedElements, NumElements);

            return clone;
        }

        public override StructuredList Join(params StructuredList[] lists)
        {
            int count = TypedElements.Length;
            for (int i = 0; i < lists.Length; i++)
            {
                if (Type != lists[i].Type)
                {
                    Log.Warning($"StructuredList.Join: trying to join different structure types, skipping at index {i}.");
                    continue;
                }

                count += lists[i].NumElements;
            }

            var newList = new StructuredList<T>(count);
            Array.Copy(TypedElements, newList.TypedElements, NumElements);
            int startIndex = TypedElements.Length;
            foreach (var sourceList in lists)
            {
                if (Type != sourceList.Type)
                {
                    continue;
                }

                var sourceArray = (T[])sourceList.Elements;
                var numElements = sourceList.NumElements;
                Array.Copy(sourceArray, 0, newList.TypedElements, startIndex, numElements);
                startIndex += numElements;
            }

            return newList;
        }

        public override void Insert(int index, object obj)
        {
            var objType = obj.GetType();
            if (objType != Type)
            {
                Log.Warning($"StructuredList.Insert: trying to insert element with type '{objType.Name} but expecting type: {Type.Name}. Skipping insertion.");
                return;
            }

            var newArray = new T[TypedElements.Length + 1];
            Array.Copy(TypedElements, newArray, index);
            newArray[index] = (T)obj;
            Array.Copy(TypedElements, index, newArray, index + 1, NumElements - index);
            TypedElements = newArray;
        }

        public override void Remove(int index)
        {
            if (index < 0 || index >= NumElements)
            {
                Log.Warning($"StructuredList.Remove: invalid index {index}");
                return;
            }

            var newArray = new T[TypedElements.Length - 1];
            Array.Copy(TypedElements, newArray, index);
            Array.Copy(TypedElements, index + 1, newArray, index, NumElements - index - 1);
            TypedElements = newArray;
        }

        public override void SetLength(int length)
        {
            if (length < 0)
            {
                Log.Error($"Cannot set length of structured list to negative value {length}");
                return;
            }

            if (length == TypedElements.Length)
                return;

            var newArray = new T[length];
            var matchingElementCount = Math.Min(length, TypedElements.Length);

            Array.Copy(TypedElements, newArray, matchingElementCount);
            TypedElements = newArray;
        }

        public override void Write(JsonTextWriter writer)
        {
            writer.Formatting = Formatting.Indented;
            writer.WriteStartObject();
            writer.WritePropertyName("StructuredList");
            writer.WriteStartArray();

            var fieldInfos = Type.GetFields();

            foreach (var entry in TypedElements)
            {
                writer.WriteStartObject();

                foreach (var fieldInfo in fieldInfos)
                {
                    var name = fieldInfo.Name;
                    writer.WritePropertyName(name);
                    var value = fieldInfo.GetValue(entry);
                    TypeValueToJsonConverters.Entries[fieldInfo.FieldType](writer, value);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        public override StructuredList Read(JsonTextReader reader)
        {
            var inputToken = JToken.ReadFrom(reader, LoadSettings);

            var jArray = (JArray)inputToken["StructuredList"];
            if (jArray == null)
            {
                TypedElements = Array.Empty<T>();
                return this;
            }
            var elementCount = jArray.Count;
            
            TypedElements = new T[elementCount];

            var fieldInfos = Type.GetFields();
            for (var index = 0; index < jArray.Count; index++)
            {
                TypedElements[index]= new T();
                object boxedEntry = TypedElements[index];
                
                var childJson = jArray[index];
                foreach (var fieldInfo in fieldInfos)
                {
                    var valueConverter = JsonToTypeValueConverters.Entries[fieldInfo.FieldType];
                    var valueJson = childJson[fieldInfo.Name];
                    var objValue =valueConverter(valueJson);
                    fieldInfo.SetValue(boxedEntry, objValue);
                }

                TypedElements[index] = (T)boxedEntry;
            }
            
            return this;
        }
    }

    public class StructuredListConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (writer is not JsonTextWriter textWriter)
                return;

            if (value is not StructuredList list)
                return;

            list.Write(textWriter);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader is not JsonTextReader textReader)
                return null;

            var obj = Activator.CreateInstance(objectType) as StructuredList;
            if (obj == null)
            {
                Log.Warning($"Can't create instance of {objectType}");
                return null;
            }
            
            obj.Read(textReader);
            return obj;
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}