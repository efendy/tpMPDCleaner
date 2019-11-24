using System;
using System.IO;
using System.Xml;
using System.Collections;

namespace tpMPDCleaner
{
    class Program
    {
        public bool enableDelete = false;
        public static void Main(string[] args)
        {
            Program program = new Program();
            program.validateArguments(args);
        }

        public void validateArguments(string[] args)
        {
            args = Environment.GetCommandLineArgs();
            
            if (args.Length < 4) {
                throw new Exception("Usage: EXE <0|1> <input file> <output file>\n" +
                                    "0: clean audio without deleting source segment files\n" +
                                    "1: clean audio and delete source segment files");
            }
            
            enableDelete = (args[1] == "1" ? true : false);
            
            string inputFile = args[2];
            Console.WriteLine("Source File: " + inputFile);

            string outputFile = args[3];
            Console.WriteLine("Output File: " + outputFile);

            if (String.IsNullOrEmpty(inputFile))
            {
                throw new Exception("Source file must be specified!");
            }
            if (String.IsNullOrEmpty(outputFile))
            {
                throw new Exception("Destination file must be specified!");
            }
            if (new FileInfo(inputFile).Length == 0)
            {
                throw new Exception("Source file does not exist!");
            }
            if (inputFile.Split('.')[1] != "mpd")
            {
                throw new Exception("Source file is not MPEG-DASH!");
            }
            if (outputFile.Split('.')[1] != "mpd")
            {
                throw new Exception("Destination file is not MPEG-DASH!");
            }

            optimizeEntry(inputFile, outputFile);
        }

        public void optimizeEntry(string inputFile, string outputFile)
        {
            FileInfo info = new FileInfo(inputFile);
            
            int progress = 0;
            setProgress(progress);
            XmlDocument doc = new XmlDocument();
            XmlNode root;
            try
            {
                using (XmlTextReader tr = new XmlTextReader(inputFile))
                {
                    tr.Namespaces = false;
                    doc.Load(tr);
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Failed to read source file: " + inputFile, exception);
            }
            setProgress(5);
            root = doc.DocumentElement;
            XmlNodeList nodeList;
            IEnumerator ienum;
            try
            {
                nodeList = root.SelectNodes("//Period/AdaptationSet[@contentType='audio']");
                ienum = nodeList.GetEnumerator();

                progress = 10;
                while (ienum.MoveNext())
                {
                    XmlElement adaptationSet = (XmlElement)ienum.Current;
                    XmlNodeList repList = adaptationSet.GetElementsByTagName("Representation");
                    for (int i = repList.Count - 1; i > 0; i--)
                    {
                        if (enableDelete) {
                            XmlNodeList repChildList = ((XmlElement)repList.Item(i)).GetElementsByTagName("BaseURL");
                            if (repChildList.Count > 0)
                            {
                                string segmentFile = info.DirectoryName + "/" +
                                                         ((XmlText)((XmlElement)repChildList.Item(0)).FirstChild).Data;
                                if (File.Exists(segmentFile))
                                {
                                    File.Delete(segmentFile);
                                }
                            }
                        }
                            
                        adaptationSet.RemoveChild(repList.Item(i));
                        setProgress(progress++, 75);
                    }
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Failed to process input.", exception);
            }
            setProgress(75);

            XmlTextWriter writer = new XmlTextWriter(outputFile, System.Text.Encoding.UTF8);
            writer.WriteStartDocument(true);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 2;
            createElement(doc.ChildNodes, writer);
            writer.WriteComment(" Modified by tpMPDCleaner ");
            writer.WriteEndDocument();
            writer.Close();
            setProgress(100);
        }

        private void createElement(XmlNodeList nodeList, XmlTextWriter writer)
        {
            if (nodeList != null && nodeList.Count > 0)
            {
                IEnumerator ienum = nodeList.GetEnumerator();
                while (ienum.MoveNext())
                {
                    Type type = ienum.Current.GetType();
                    if (type.Name == "XmlDeclaration")
                    {

                    }
                    else if (type.Name == "XmlElement")
                    {
                        XmlElement element = (XmlElement)ienum.Current;
                        writer.WriteStartElement(element.Name);
                        createAttribute(element.Attributes, writer);
                        createElement(element.ChildNodes, writer);
                        writer.WriteEndElement();
                    }
                    else if (type.Name == "XmlComment")
                    {
                        XmlComment comment = (XmlComment)ienum.Current;
                        writer.WriteComment(comment.Data);
                    }
                    else if (type.Name == "XmlText")
                    {
                        XmlText text = (XmlText)ienum.Current;
                        writer.WriteString(text.Data);
                    }
                }
            }
        }

        private void createAttribute(XmlAttributeCollection attributeCollection, XmlTextWriter writer)
        {
            if (attributeCollection != null)
            {
                IEnumerator ienum = attributeCollection.GetEnumerator();
                while (ienum.MoveNext())
                {
                    XmlAttribute xmlAttribute = (XmlAttribute)ienum.Current;
                    try
                    {
                        writer.WriteAttributeString(xmlAttribute.Name, xmlAttribute.Value);
                    }
                    catch (Exception exception)
                    {
                        throw new Exception("Failed to process attributes.", exception);
                    }
                }
            }
        }

        private void setProgress(int progress, int maxProgress = 100)
        {
            if (progress <= maxProgress)
            {
                Console.WriteLine("tpTime=" + progress + "%");

                if (progress >= 100)
                {
                    Console.WriteLine("Task completed!");
                }
            }
        }
    }
}
