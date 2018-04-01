﻿using Microsoft.Xml.XMLGen;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Xunit;

namespace XmlSchemaClassGenerator.Tests
{
    [TestCaseOrderer("XmlSchemaClassGenerator.Tests.PriorityOrderer", "XmlSchemaClassGenerator.Tests")]
    public class XmlTests
    {
        private static Dictionary<string, Assembly> Assemblies = new Dictionary<string, Assembly>();

        private Assembly Compile(string name, string pattern, Generator generatorPrototype = null)
        {
            if (Assemblies.ContainsKey(name)) { return Assemblies[name]; }

            var cs = new List<string>();

            var outputFolder = Path.Combine("output", name);
            Directory.CreateDirectory(outputFolder);

            generatorPrototype = generatorPrototype ?? new Generator
            {
                GenerateNullables = true,
                IntegerDataType = typeof(int),
                DataAnnotationMode = DataAnnotationMode.All,
                GenerateDesignerCategoryAttribute = false,
                EntityFramework = false,
                GenerateInterfaces = true,
                NamespacePrefix = name,
                InheritenceName = "ValueObject",
                InheritenceNamespace = "CSharpFunctionalExtensions"
            };

            var gen = new Generator
            {
                OutputFolder = outputFolder,
                NamespaceProvider = generatorPrototype.NamespaceProvider,
                Log = f => cs.Add(f),
                GenerateNullables = generatorPrototype.GenerateNullables,
                IntegerDataType = generatorPrototype.IntegerDataType,
                DataAnnotationMode = generatorPrototype.DataAnnotationMode,
                GenerateDesignerCategoryAttribute = generatorPrototype.GenerateDesignerCategoryAttribute,
                EntityFramework = generatorPrototype.EntityFramework,
                GenerateInterfaces = generatorPrototype.GenerateInterfaces,
                MemberVisitor = generatorPrototype.MemberVisitor,
                InheritenceName = generatorPrototype.InheritenceName,
                InheritenceNamespace = generatorPrototype.InheritenceNamespace,
                ValueTypeEnable = generatorPrototype.ValueTypeEnable,
            };

            var files = Glob.Glob.ExpandNames(pattern);

            gen.Generate(files);

            var provider = CodeDomProvider.CreateProvider("CSharp");
            var assemblies = new[]
            {
                "System.dll",
                "System.Core.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll",
                "System.Xml.Serialization.dll",
                "System.ServiceModel.dll",
                "System.ComponentModel.DataAnnotations.dll",
            };

            var binFolder = Path.Combine(outputFolder, "bin");
            Directory.CreateDirectory(binFolder);
            var results = provider.CompileAssemblyFromFile(new CompilerParameters(assemblies, Path.Combine(binFolder, name + ".dll")), cs.ToArray());

            Assert.False(results.Errors.HasErrors, string.Join("\n", results.Output.Cast<string>()));
            Assert.False(results.Errors.HasWarnings, string.Join("\n", results.Output.Cast<string>()));
            Assert.NotNull(results.CompiledAssembly);

            var assembly = Assembly.Load(results.CompiledAssembly.GetName());

            Assemblies[name] = assembly;

            return assembly;
        }

        const string IS24Pattern = @"xsd\is24\*\*.xsd";
        const string IS24ImmoTransferPattern = @"xsd\is24immotransfer\is24immotransfer.xsd";
        const string WadlPattern = @"xsd\wadl\wadl.xsd";
        const string ClientPattern = @"xsd\client\client.xsd";
        const string IataPattern = @"xsd\iata\????[^_][^_]?[^-]*.xsd";

        private Generator GetValueObjectGeneratorStartup()
            => new Generator
            {
                GenerateNullables = true,
                IntegerDataType = typeof(int),
                DataAnnotationMode = DataAnnotationMode.All,
                GenerateDesignerCategoryAttribute = false,
                EntityFramework = false,
                GenerateInterfaces = true,
                ValueTypeEnable = true,
                InheritenceName = "ValueObject",
                InheritenceNamespace = "CSharpFunctionalExtensions",
            };

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void CanDeserializeSampleClientXml()
        {
            Compile("Client", ClientPattern);
            TestSamples("Client", ClientPattern);
        }

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void CanDeserializeSampleClientWithToValueObjectXml()
        {
            Compile("Client", ClientPattern, GetValueObjectGeneratorStartup());
            TestWithValueTypeInheritenceSamples("Client", ClientPattern);
        }

        [Fact, TestPriority(2)]
        [UseCulture("en-US")]
        public void CanDeserializeSampleIS24RestApiXml()
        {
            Compile("IS24RestApi", IS24Pattern);
            TestSamples("IS24RestApi", IS24Pattern);
        }

        [Fact, TestPriority(2)]
        [UseCulture("en-US")]
        public void CanDeserializeSampleIS24RestApiWithToValueObjectXml()
        {
            var config = GetValueObjectGeneratorStartup();
            config.NamespacePrefix = "IS24RestApi";
            Compile("IS24RestApi", IS24Pattern, config);
            TestWithValueTypeInheritenceSamples("IS24RestApi", IS24Pattern);
        }

        [Fact, TestPriority(3)]
        [UseCulture("en-US")]
        public void CanDeserializeSampleWadlXml()
        {
            Compile("Wadl", WadlPattern, new Generator
            {
                EntityFramework = true,
                DataAnnotationMode = DataAnnotationMode.All,
                NamespaceProvider = new Dictionary<NamespaceKey, string> { { new NamespaceKey("http://wadl.dev.java.net/2009/02"), "Wadl" } }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Wadl" }.NamespaceProvider.GenerateNamespace),
                MemberVisitor = (member, model) => { }
            });
            TestSamples("Wadl", WadlPattern);
        }

        [Fact, TestPriority(3)]
        [UseCulture("en-US")]
        public void CanDeserializeSampleWadlWithToValueObjectXml()
        {
            var config = GetValueObjectGeneratorStartup();
            Compile("Wadl", WadlPattern, new Generator
            {
                EntityFramework = true,
                DataAnnotationMode = DataAnnotationMode.All,
                NamespaceProvider = new Dictionary<NamespaceKey, string> { { new NamespaceKey("http://wadl.dev.java.net/2009/02"), "Wadl" } }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Wadl" }.NamespaceProvider.GenerateNamespace),
                MemberVisitor = (member, model) => { },
                InheritenceName = config.InheritenceName,
                InheritenceNamespace = config.InheritenceNamespace,
                ValueTypeEnable = true
            });
            TestWithValueTypeInheritenceSamples("Wadl", WadlPattern);
        }

        [Fact, TestPriority(4)]
        [UseCulture("en-US")]
        public void CanDeserializeSampleIS24ImmoTransferXml()
        {
            Compile("IS24ImmoTransfer", IS24ImmoTransferPattern);
            TestSamples("IS24ImmoTransfer", IS24ImmoTransferPattern);
        }

        [Fact, TestPriority(4)]
        [UseCulture("en-US")]
        public void CanDeserializeSampleIS24ImmoTransferToValueObjectXml()
        {
            var config = GetValueObjectGeneratorStartup();
            config.NamespacePrefix = "IS24ImmoTransfer";
            Compile("IS24ImmoTransfer", IS24ImmoTransferPattern, config);
            TestWithValueTypeInheritenceSamples("IS24ImmoTransfer", IS24ImmoTransferPattern);
        }

        [Fact, TestPriority(5)]
        [UseCulture("en-US")]
        public void CanDeserializeSampleIataXml()
        {
            Compile("Iata", IataPattern, new Generator
            {
                EntityFramework = true,
                DataAnnotationMode = DataAnnotationMode.All,
                NamespaceProvider = new Dictionary<NamespaceKey, string> { { new NamespaceKey(""), "XmlSchema" }, { new NamespaceKey("http://www.iata.org/IATA/EDIST/2017.2"), "Iata" } }
                    .ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Wadl" }.NamespaceProvider.GenerateNamespace),
                MemberVisitor = (member, model) => { },
                GenerateInterfaces = true
            });
            TestSamples("Iata", IataPattern);
        }

        [Fact, TestPriority(5)]
        [UseCulture("en-US")]
        public void CanDeserializeSampleIataToValueObjectXml()
        {
            var gen = GetValueObjectGeneratorStartup();
            Compile("Iata", IataPattern, new Generator
            {
                EntityFramework = true,
                DataAnnotationMode = DataAnnotationMode.All,
                NamespaceProvider = new Dictionary<NamespaceKey, string> { { new NamespaceKey(""), "XmlSchema" }, { new NamespaceKey("http://www.iata.org/IATA/EDIST/2017.2"), "Iata" } }
                    .ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Wadl" }.NamespaceProvider.GenerateNamespace),
                MemberVisitor = (member, model) => { },
                GenerateInterfaces = true,
                InheritenceName = gen.InheritenceName,
                InheritenceNamespace = gen.InheritenceNamespace,
                ValueTypeEnable = true
            });
            TestWithValueTypeInheritenceSamples("Iata", IataPattern);
        }

        private void TestSamples(string name, string pattern)
        {
            Assemblies.TryGetValue(name, out Assembly assembly);
            Assert.NotNull(assembly);
            DeserializeSampleXml(pattern, assembly);
        }

        private void TestWithValueTypeInheritenceSamples(string name, string pattern)
        {
            Assemblies.TryGetValue(name, out Assembly assembly);
            Assert.NotNull(assembly);
            DeserializeSampleXml(pattern, assembly, true);
        }

        private void DeserializeSampleXml(string pattern, Assembly assembly)
            => DeserializeSampleXml(pattern, assembly, false);

        private void DeserializeSampleXml(string pattern, Assembly assembly, bool valueTypeEnabled)
        {
            var files = Glob.Glob.ExpandNames(pattern);

            var set = new XmlSchemaSet();

            var schemas = files.Select(f => XmlSchema.Read(XmlReader.Create(f), (s, e) =>
            {
                Assert.True(false, e.Message);
            }));

            foreach (var s in schemas)
            {
                set.Add(s);
            }

            set.Compile();

            foreach (var rootElement in set.GlobalElements.Values.Cast<XmlSchemaElement>().Where(e => !e.IsAbstract))
            {
                var type = FindType(assembly, rootElement.QualifiedName);
                var serializer = new XmlSerializer(type);
                var generator = new XmlSampleGenerator(set, rootElement.QualifiedName);
                var sb = new StringBuilder();
                using (var xw = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true }))
                {
                    // generate sample xml
                    generator.WriteXml(xw);
                    var xml = sb.ToString();

                    File.WriteAllText("xml.xml", xml);

                    // deserialize from sample
                    var sr = new StringReader(xml);
                    var o = serializer.Deserialize(sr);

                    // serialize back to xml
                    var xml2 = Serialize(serializer, o);

                    File.WriteAllText("xml2.xml", xml2);

                    // validate serialized xml
                    XmlReaderSettings settings = new XmlReaderSettings
                    {
                        ValidationType = ValidationType.Schema,
                        Schemas = set
                    };

                    settings.ValidationEventHandler += (s, e) =>
                    {
                        // generator doesn't generate valid values where pattern restrictions exist, e.g. email
                        if (!e.Message.Contains("The Pattern constraint failed"))
                        {
                            Assert.True(false, e.Message);
                        }
                    };

                    XmlReader reader = XmlReader.Create(new StringReader(xml2), settings);
                    while (reader.Read()) ;

                    // deserialize again
                    sr = new StringReader(xml2);
                    var o2 = serializer.Deserialize(sr);
                    if (!valueTypeEnabled)
                        AssertEx.Equal(o, o2);
                    else
                        Assert.Equal(o, o2);
                }
            }
        }

        private Type FindType(Assembly assembly, XmlQualifiedName xmlQualifiedName)
        {
            return assembly.GetTypes()
                .Single(t => t.CustomAttributes.Any(a => a.AttributeType == typeof(XmlRootAttribute)
                    && a.ConstructorArguments.Any(n => (string)n.Value == xmlQualifiedName.Name)
                    && a.NamedArguments.Any(n => n.MemberName == "Namespace" && (string)n.TypedValue.Value == xmlQualifiedName.Namespace)));
        }

        static string[] Classes = new[] { "ApartmentBuy",
                "ApartmentRent",
                "AssistedLiving",
                "CompulsoryAuction",
                "GarageBuy",
                "GarageRent",
                "Gastronomy",
                "HouseBuy",
                "HouseRent",
                "HouseType",
                "Industry",
                "Investment",
                "LivingBuySite",
                "LivingRentSite",
                "Office",
                "SeniorCare",
                "ShortTermAccommodation",
                "SpecialPurpose",
                "Store",
                "TradeSite" };

        [Fact, TestPriority(6)]
        public void ProducesSameXmlAsXsd()
        {
            var assembly = Compile("IS24RestApi", IS24Pattern);

            foreach (var c in Classes)
            {
                var t1 = assembly.GetTypes().SingleOrDefault(t => t.Name == c && t.Namespace.StartsWith("IS24RestApi.Offer.Realestates"));
                Assert.NotNull(t1);
                var t2 = Assembly.GetExecutingAssembly().GetTypes().SingleOrDefault(t => t.Name == c && t.Namespace == "IS24RestApi.Xsd");
                Assert.NotNull(t2);
                var f = char.ToLower(c[0]) + c.Substring(1);
                TestCompareToXsd(t1, t2, f);
            }
        }

        void TestCompareToXsd(Type t1, Type t2, string file)
        {
            foreach (var suffix in new[] { "max", "min" })
            {
                var serializer1 = new XmlSerializer(t1);
                var serializer2 = new XmlSerializer(t2);
                var xml = ReadXml(string.Format("{0}_{1}", file, suffix));
                var o1 = serializer1.Deserialize(new StringReader(xml));
                var o2 = serializer2.Deserialize(new StringReader(xml));
                var x1 = Serialize(serializer1, o1);
                var x2 = Serialize(serializer2, o2);

                File.WriteAllText("x1.xml", x1);
                File.WriteAllText("x2.xml", x2);

                Assert.Equal(x2, x1);
            }
        }

        [Fact, TestPriority(7)]
        public void CanSerializeAndDeserializeAllExampleXmlFiles()
        {
            var assembly = Compile("IS24RestApi", IS24Pattern);

            foreach (var c in Classes)
            {
                var t1 = assembly.GetTypes().SingleOrDefault(t => t.Name == c && t.Namespace.StartsWith("IS24RestApi.Offer.Realestates"));
                Assert.NotNull(t1);
                var f = char.ToLower(c[0]) + c.Substring(1);
                TestRoundtrip(t1, f);
            }
        }

        void TestRoundtrip(Type t, string file)
        {
            var serializer = new XmlSerializer(t);

            foreach (var suffix in new[] { "min", "max" })
            {
                var xml = ReadXml(string.Format("{0}_{1}", file, suffix));

                var deserializedObject = serializer.Deserialize(new StringReader(xml));

                var serializedXml = Serialize(serializer, deserializedObject);

                var deserializedXml = serializer.Deserialize(new StringReader(serializedXml));
                AssertEx.Equal(deserializedObject, deserializedXml);
            }
        }

        string Serialize(XmlSerializer serializer, object o)
        {
            var sw = new StringWriter();
            var ns = new XmlSerializerNamespaces();
            ns.Add("", null);
            serializer.Serialize(sw, o, ns);
            var serializedXml = sw.ToString();
            return serializedXml;
        }

        string ReadXml(string name)
        {
            var folder = Directory.GetCurrentDirectory();
            var xml = File.ReadAllText(string.Format(@"{0}\xml\{1}.xml", folder, name));
            return xml;
        }
    }
}
