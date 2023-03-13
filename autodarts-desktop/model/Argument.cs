using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;


namespace autodarts_desktop.model
{
    /// <summary>
    /// An argument, usable by AppBase
    /// </summary>
    public class Argument
    {
        // ATTRIBUTES

        public string Name { get; set; }

        public bool Required { get; set; }

        public string Type { get; set; }

        public string? Section { get; set; }

        public string? Description { get; set; }

        public string? NameHuman { get; set; }

        public string? RequiredOnArgument { get; set; }

        public bool EmptyAllowedOnRequired { get; set; }

        public bool IsRuntimeArgument { get; set; }

        public bool IsMulti { get; set; }

        public string? Value { get; set; }

        public Dictionary<string, string>? ValueMapping { get; set; }

        [JsonIgnore]
        public string RangeBy { get; private set; }

        [JsonIgnore]
        public string RangeTo { get; private set; }


        public const string TypeString = "string";
        public const string TypeFloat = "float";
        public const string TypeInt = "int";
        public const string TypeBool = "bool";
        public const string TypeFile = "file";
        public const string TypePath = "path";
        public const string TypePassword = "password";
        public const string TypeSelection = "selection";
        public const string RangeDelimitter = "..";
        public const char RangeBorderStart = '[';
        public const char RangeBorderEnd = ']';
        public const char RangeSeparator = '|';



        // METHODS

        public Argument(string name,
                        string type,            
                        bool required, 
                        string? section = null, 
                        string? description = null, 
                        string? nameHuman = null,
                        string? requiredOnArgument = null,
                        bool emptyAllowedOnRequired = false,
                        bool isRuntimeArgument = false,
                        bool isMulti = false,
                        string? value = null,
                        Dictionary<string, string>? valueMapping = null 
                )
        {
            Name = name;
            Required = required;
            Type = type.ToLower();
            Section = section;
            Description = description;
            NameHuman = String.IsNullOrEmpty(nameHuman) ? Name : nameHuman;
            RequiredOnArgument = requiredOnArgument;
            EmptyAllowedOnRequired = emptyAllowedOnRequired;
            IsRuntimeArgument = isRuntimeArgument;
            IsMulti = isMulti;
            Value = value;
            ValueMapping = valueMapping;

            ValidateType();
        }

        public string? MappedValue()
        {
            if(ValueMapping != null)
            {
                foreach (var item in ValueMapping)
                {
                    if (Value == item.Key) return item.Value;
                }
            }

            return Value;
        }

        public bool ShouldSerializeValue()
        {
            return !IsRuntimeArgument;
        }

        public void Validate()
        {
            // Validate Type & Value

            if (String.IsNullOrEmpty(Value) && !EmptyAllowedOnRequired && Required) ThrowException("is required");

            if (Type.StartsWith(TypeString))
            {
                ValidateString();
            }
            else if(Type.StartsWith(TypeFloat))
            {
                ValidateFloat();
            }
            else if (Type.StartsWith(TypeInt))
            {
                ValidateInt();
            }
            else if (Type == TypeBool)
            {
                ValidateBool();
            }
            else if (Type == TypeFile)
            {
                ValidateFile();
            }
            else if (Type == TypePath)
            {
                ValidatePath();
            }
            else if (Type == TypePassword)
            {
                ValidatePassword();
            }
            else if (Type.StartsWith(TypeSelection))
            {
                ValidateSelection();
            }
            else
            {
                ThrowException($"Invalid type {Type}");
            }

        }

        public string GetTypeClear()
        {
            if (Type.StartsWith(TypeString))
            {
                return TypeString;
            }
            else if (Type.StartsWith(TypeFloat))
            {
                return TypeFloat;
            }
            else if (Type.StartsWith(TypeInt))
            {
                return TypeInt;
            }
            else if (Type == TypeBool)
            {
                return TypeBool;
            }
            else if (Type == TypeFile)
            {
                return TypeFile;
            }
            else if (Type == TypePath)
            {
                return TypePath;
            }
            else if (Type == TypePassword)
            {
                return TypePassword;
            }
            else if (Type.StartsWith(TypeSelection))
            {
                return TypeSelection;
            }
            return "invalidType";
        }

        public void ValidateType()
        {
            try
            {
                if (Type.StartsWith(TypeString))
                {
                    if (Type.Length > TypeString.Length) ValidateRange(TypeString);
                    return;
                }
                else if (Type.StartsWith(TypeFloat))
                {
                    if (Type.Length > TypeFloat.Length) ValidateRange(TypeFloat);
                    return;
                }
                else if (Type.StartsWith(TypeInt))
                {
                    if (Type.Length > TypeInt.Length) ValidateRange(TypeInt);
                    return;
                }
                else if (Type == TypeBool ||
                        Type == TypeFile ||
                        Type == TypePath ||
                        Type == TypePassword ||
                        Type.StartsWith(TypeSelection))
                {
                    return;
                }
                throw new Exception($"Argument-Type '{Type}' is invalid: " + NameHuman);
            }
            catch(Exception ex)
            {
                throw new Exception($"Argument-Type '{Type}' is invalid: " + NameHuman + " " + ex.Message);
            }
        }


        private void ValidateValue(string type)
        {
            // => float[0..10]
            var range = Type.Substring(type.Length, Type.Length - type.Length);
            //Console.WriteLine($"{type}: " + range);

            // => [0 10]
            var rangeSplitted = range.Split(RangeDelimitter);

            if (rangeSplitted.Length != 2) throw new Exception();
            if (rangeSplitted[0][0] != RangeBorderStart) throw new Exception();
            if (rangeSplitted[1][rangeSplitted[1].Length - 1] != RangeBorderEnd) throw new Exception();

            RangeBy = rangeSplitted[0].Remove(0, 1);
            RangeTo = rangeSplitted[1].Remove(rangeSplitted[1].Length - 1);

            //Console.WriteLine($"{type}-range-by: " + RangeBy);
            //Console.WriteLine($"{type}-range-to: " + RangeTo);

            if(type == TypeString)
            {
                if (Value.Length < int.Parse(RangeBy) || Value.Length > int.Parse(RangeTo)) throw new Exception($"Out of range ({RangeBy} to {RangeTo})");
            }
            else if(type == TypeFloat)
            {
                if (float.Parse(Value, new CultureInfo("en-us")) < float.Parse(RangeBy, new CultureInfo("en-us")) || float.Parse(Value, new CultureInfo("en-us")) > float.Parse(RangeTo, new CultureInfo("en-us"))) throw new Exception($"Out of range ({RangeBy} to {RangeTo})");
            }
            else if(type == TypeInt)
            {
                if (int.Parse(Value) < int.Parse(RangeBy) || int.Parse(Value) > int.Parse(RangeTo)) throw new Exception($"Out of range ({RangeBy} to {RangeTo})");
            }
        }

        private void ValidateRange(string type)
        {
            // => float[0..10]
            var range = Type.Substring(type.Length, Type.Length - type.Length);

            // => [0 10]
            var rangeSplitted = range.Split(RangeDelimitter);

            if (rangeSplitted.Length != 2) throw new Exception();
            if (rangeSplitted[0][0] != RangeBorderStart) throw new Exception();
            if (rangeSplitted[1][rangeSplitted[1].Length - 1] != RangeBorderEnd) throw new Exception();

            RangeBy = rangeSplitted[0].Remove(0, 1);
            RangeTo = rangeSplitted[1].Remove(rangeSplitted[1].Length - 1);
        }

        private void ValidateString()
        {
            if(Type.Length > TypeString.Length)
            {
                try
                {
                    ValidateValue(TypeString);
                }
                catch(Exception ex)
                {
                    ThrowException($"Invalid {TypeString}: {Value}. " + ex.Message);
                }
            }
        }

        private void ValidateFloat()
        {
            try
            {
                float.Parse(Value);
                ValidateValue(TypeFloat);
            }
            catch (Exception ex)
            {
                ThrowException($"Invalid {TypeFloat}: {Value}. " + ex.Message);
            }
        }

        private void ValidateInt()
        {
            try
            {
                int.Parse(Value);
                ValidateValue(TypeInt);
            }
            catch (Exception ex)
            {
                ThrowException($"Invalid {TypeInt}: {Value}. " + ex.Message);
            }
        }

        private void ValidateBool()
        {
            try
            {
                bool.Parse(Value);
            }
            catch (Exception ex)
            {
                ThrowException($"Invalid {TypeBool}: {Value}. " + ex.Message);
            }
        }

        private void ValidateFile()
        {
            try
            {
                new FileInfo(Value);
            }
            catch (Exception ex)
            {
                ThrowException($"Invalid {TypeFile}: {Value}. " + ex.Message);
            }
        }

        private void ValidatePath()
        {
            try
            {
                Path.GetFullPath(Value);
            }
            catch (Exception ex)
            {
                ThrowException($"Invalid {TypePath}: {Value}. " + ex.Message);
            }
        }

        private void ValidatePassword()
        {
            // skip - no rules
        }

        private void ValidateSelection()
        {
            try
            {
                // TODO: improve logic..
                // => selection[fish | dog | cat]
                if(!Type.Contains(Value)) throw new Exception($"Out of selection");
            }
            catch (Exception ex)
            {
                ThrowException($"Invalid {TypeSelection}: {Value}. " + ex.Message);
            }
        }

        private void ThrowException(string message)
        {
            var ex = new ArgumentException(Configuration.ArgumentErrorKey + NameHuman + ": " + message);
            ex.Data.Add("argument", this);
            throw ex;
        }

    }
}
