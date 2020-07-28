
using Horus.Generator.Models;
using MigraDoc.DocumentObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Horus.Generator.Builders
{
    public abstract class DocumentBuilder : IDocumentBuilder
    {
        protected Document document;
        protected readonly GeneratorHeader header;
        protected readonly GeneratorDocument generatorDocument;

        public DocumentBuilder(GeneratorHeader header, GeneratorDocument generatorDocument)
        {

            this.header = header;
            this.generatorDocument = generatorDocument;
        }

        public abstract Document Build();

        protected virtual Paragraph ApplyParagraphProperties(Paragraph paragraph, Dictionary<string, string> properties)
        {
            if (paragraph == null) throw new Exception("Attempt to apply properties to a null paragraph");
            if (properties == null) return paragraph;
            foreach (var item in properties)
            {
                switch (item.Key)
                {
                    case "Paragraph.Format.Font.Size":
                        paragraph.Format.Font.Size = Int32.Parse(item.Value);
                        break;
                    case "Paragraph.Format.Font.Name":
                        paragraph.Format.Font.Name = item.Value;
                        break;
                    case "Paragraph.Format.SpaceAfter":
                        paragraph.Format.SpaceAfter = Int32.Parse(item.Value);
                        break;
                    default:
                        break;
                }

            }
            return paragraph;
        }

        protected virtual void DefineStyles()
        {
            // Get the predefined style Normal.
            Style style = this.document.Styles["Normal"];
            // Because all styles are derived from Normal, the next line changes the 
            // font of the whole document. Or, more exactly, it changes the font of
            // all styles and paragraphs that do not redefine the font.
            style.Font.Name = "Verdana";

            style = this.document.Styles[StyleNames.Header];
            style.ParagraphFormat.AddTabStop("16cm", TabAlignment.Right);

            style = this.document.Styles[StyleNames.Footer];
            style.ParagraphFormat.AddTabStop("8cm", TabAlignment.Center);

            // Create a new style called Table based on style Normal
            style = this.document.Styles.AddStyle("Table", "Normal");
            style.Font.Name = "Verdana";
            
            style.Font.Size = 9;

            // Create a new style called Reference based on style Normal
            style = this.document.Styles.AddStyle("Reference", "Normal");
            style.ParagraphFormat.SpaceBefore = "5mm";
            style.ParagraphFormat.SpaceAfter = "5mm";
            style.ParagraphFormat.TabStops.AddTabStop("16cm", TabAlignment.Right);
        }
    }
}
