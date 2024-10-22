﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JSONScript.Common
{
    public static class FileDeserializer
    {
        public static FileMethodSettings DeserializeMethod(string filePath, out MemberAttributes attributes)
        {
            attributes = 0;
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File \"{filePath}\" not found.");
            }
            FileMethodSettings deserialized;
            try
            {
                deserialized = JsonSerializer.Deserialize<FileMethodSettings>(File.ReadAllText(filePath));
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"File \"{filePath}\" contains invalid or malformed JSON.\nReason:\n{ex.Message}");
            }
            if (deserialized.AccessModifier == null)
            {
                throw new ArgumentException($"File \"{filePath}\" must specify a valid access modifier.");
            }
            if (!Main.ValidAccessModifiers.Contains(deserialized.AccessModifier.ToLower()))
            {
                throw new ArgumentException($"File \"{filePath}\": \"${deserialized.AccessModifier.ToLower()}\" is not a valid access modifier.");
            }
            if (deserialized.ParameterNames?.Count != deserialized.ParameterTypes?.Count)
            {
                throw new ArgumentException($"File \"{filePath}\": each parameter must have a matching name or type.");
            }
            if (deserialized.Name == null)
            {
                throw new ArgumentException($"File \"{filePath}\" must specify a valid method name.");
            }
            if (deserialized.Namespace == null)
            {
                throw new ArgumentException($"File \"{filePath}\" must specify a valid method namespace (include the class name).");
            }
            if (deserialized.ParameterDefaultValues?.Count > deserialized.ParameterTypes?.Count)
            {
                throw new ArgumentException($"File \"{filePath}\": too many default parameter values defined.");
            }
            if (deserialized.Code == null)
            {
                throw new ArgumentException($"File \"{filePath}\": code must be present to run.");
            }
            attributes = GetAttributes(deserialized.AccessModifier, deserialized.IsStatic);
            return deserialized;
        }

        public static FileClassSettings DeserializeClass(string filePath, out MemberAttributes attributes)
        {
            attributes = 0;
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File \"{filePath}\" not found.");
            }
            FileClassSettings deserialized;
            try
            {
                deserialized = JsonSerializer.Deserialize<FileClassSettings>(File.ReadAllText(filePath));
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"File \"{filePath}\" contains invalid or malformed JSON.\nReason:\n{ex.Message}");
            }
            if (deserialized.AccessModifier == null)
            {
                throw new ArgumentException($"File \"{filePath}\" must specify a valid access modifier.");
            }
            if (!Main.ValidAccessModifiers.Contains(deserialized.AccessModifier.ToLower()))
            {
                throw new ArgumentException($"File \"{filePath}\": \"${deserialized.AccessModifier.ToLower()}\" is not a valid access modifier.");
            }
            if (deserialized.Name == null)
            {
                throw new ArgumentException($"File \"{filePath}\" must specify a valid class name.");
            }
            if (deserialized.Namespace == null)
            {
                throw new ArgumentException($"File \"{filePath}\" must specify a valid class namespace.");
            }
            attributes = GetAttributes(deserialized.AccessModifier, deserialized.IsStatic);
            return deserialized;
        }

        public static FileNamespaceSettings DeserializeNamespace(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File \"{filePath}\" not found.");
            }
            FileNamespaceSettings deserialized;
            try
            {
                deserialized = JsonSerializer.Deserialize<FileNamespaceSettings>(File.ReadAllText(filePath));
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"File \"{filePath}\" contains invalid or malformed JSON.\nReason:\n{ex.Message}");
            }
            if (deserialized.Namespace == null)
            {
                throw new ArgumentException($"File \"{filePath}\" must specify a valid namespace to create.");
            }
            return deserialized;
        }

        public static FileFieldSettings DeserializeField(string filePath, out MemberAttributes attributes)
        {
            attributes = 0;
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File \"{filePath}\" not found.");
            }
            FileFieldSettings deserialized;
            try
            {
                deserialized = JsonSerializer.Deserialize<FileFieldSettings>(File.ReadAllText(filePath));
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"File \"{filePath}\" contains invalid or malformed JSON.\nReason:\n{ex.Message}");
            }
            if (deserialized.AccessModifier == null)
            {
                throw new ArgumentException($"File \"{filePath}\" must specify a valid access modifier.");
            }
            if (!Main.ValidAccessModifiers.Contains(deserialized.AccessModifier.ToLower()))
            {
                throw new ArgumentException($"File \"{filePath}\": \"${deserialized.AccessModifier.ToLower()}\" is not a valid access modifier.");
            }
            if (deserialized.Name == null)
            {
                throw new ArgumentException($"File \"{filePath}\" must specify a valid field name.");
            }
            if (deserialized.Namespace == null)
            {
                throw new ArgumentException($"File \"{filePath}\" must specify a valid field namespace.");
            }
            attributes = GetAttributes(deserialized.AccessModifier, deserialized.IsStatic);
            return deserialized;
        }

        private static MemberAttributes GetAttributes(string accessModifier, bool isStatic)
        {
            MemberAttributes attributes = 0;
            switch (accessModifier.ToLower())
            {
                case "public":
                    attributes |= MemberAttributes.Public;
                    break;
                case "private":
                    attributes |= MemberAttributes.Private;
                    break;
                case "protected":
                    attributes |= MemberAttributes.FamilyOrAssembly;
                    break;
                case "internal":
                    attributes |= MemberAttributes.FamilyAndAssembly;
                    break;
            }
            if (isStatic)
            {
                attributes |= MemberAttributes.Static;
            }
            return attributes;
        }
    }

    public class FileMethodSettings
    {
        public string AccessModifier { get; set; }

        public string ReturnType { get; set; } = "System.Void";

        public bool IsStatic { get; set; } = false;

        public string Name { get; set; }

        public string Namespace { get; set; }

        public List<string> ParameterTypes { get; set; } = new List<string>();

        public List<string> ParameterNames { get; set; } = new List<string>();

        public List<string> ParameterDefaultValues { get; set; } = new List<string>();

        public string Code { get; set; }
    }

    public class FileClassSettings
    {
        public string AccessModifier { get; set; }

        public bool IsStatic { get; set; } = false;

        public string Name { get; set; }

        public string Namespace { get; set; }

        public List<string> Implements { get; set; } = new List<string>();
    }

    public class FileNamespaceSettings
    {
        public string Namespace { get; set; }

        public List<string> Implements { get; set; } = new List<string>();
    }

    public class FileFieldSettings
    {
        public string Name { get; set; }

        public string Namespace { get; set; }

        public bool IsStatic { get; set; } = false;

        public string Type { get; set; } = "System.Int32";

        public string DeclarationValue { get; set; } = "";

        public string AccessModifier { get; set; }
    }

    public class FileCompilerSettings
    {
        public string EntryNamespace { get; set; }

        public string EntryMethod { get; set; }

        public string AssemblyName { get; set; }

        public bool SilentCompilation { get; set; }

        public bool VisualizeOnError { get; set; }
    }
}
