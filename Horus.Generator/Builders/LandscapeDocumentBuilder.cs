#region MigraDoc - Creating Documents on the Fly
//
// Authors:
//   PDFsharp Team (mailto:PDFsharpSupport@pdfsharp.de)
//
// Copyright (c) 2001-2009 empira Software GmbH, Cologne (Germany)
//
// http://www.pdfsharp.com
// http://www.migradoc.com
// http://sourceforge.net/projects/pdfsharp
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.DocumentObjectModel.Shapes;
using Horus.Generator.Models;
using System.Collections.Generic;
using System;

namespace Horus.Generator.Builders
{
    /// <summary>
    /// Creates the invoice form.
    /// </summary>
    public class LandscapeDocumentBuilder : DocumentBuilder
    {

        TextFrame addressFrame;
        Table table;

        public LandscapeDocumentBuilder(GeneratorHeader header, GeneratorDocument generatorDocument) : base(header, generatorDocument){}
        
        public override Document Build()
        {
            // Create a new MigraDoc document
            this.document = new Document();
            this.document.Info.Title = "A sample invoice";
            this.document.Info.Subject = "Demonstrates how to create an invoice.";
            this.document.Info.Author = "Stefan Lange";

            DefineStyles();
            CreatePage();
            FillContent();

            return this.document;
        }

        /// <summary>
        /// Creates the static parts of the invoice.
        /// </summary>
        void CreatePage()
        {
            // Each MigraDoc document needs at least one section.
            Section section = this.document.AddSection();
            section.PageSetup.Orientation = Orientation.Landscape;
            // Put a logo in the header
            Image image = section.Headers.Primary.AddImage(header.LogoFile);
            image.Height = "2.5cm";
            image.LockAspectRatio = true;
            image.RelativeVertical = RelativeVertical.Line;
            image.RelativeHorizontal = RelativeHorizontal.Margin;
            image.Top = ShapePosition.Top;
            image.Left = ShapePosition.Right;
            image.WrapFormat.Style = WrapStyle.Through;

            // Create footer
            Paragraph paragraph = section.Footers.Primary.AddParagraph();
            paragraph.AddText(header.SupplierFullName);
            paragraph.Format.Font.Size = 9;
            paragraph.Format.Alignment = ParagraphAlignment.Center;

            // Create the text frame for the address
            this.addressFrame = section.AddTextFrame();
            this.addressFrame.Height = "3.0cm";
            this.addressFrame.Width = "7.0cm";
            this.addressFrame.Left = ShapePosition.Right;
            this.addressFrame.RelativeHorizontal = RelativeHorizontal.Margin;
            this.addressFrame.Top = "5.0cm";
            this.addressFrame.RelativeVertical = RelativeVertical.Page;

            // Put sender in address frame
            paragraph = this.addressFrame.AddParagraph(header.SupplierName);
            paragraph.Format.Font.Name = "Times New Roman";
            paragraph.Format.Font.Size = 7;
            paragraph.Format.SpaceAfter = 3;
            paragraph = ApplyParagraphProperties(paragraph, header.SupplierNameProperties);

            // Add the print date field
            paragraph = section.AddParagraph();
            paragraph.Format.SpaceBefore = "8cm";
            paragraph.Style = "Reference";
            paragraph.AddFormattedText($"{header.DocumentType} {generatorDocument.DocumentNumber}", TextFormat.Bold);
            paragraph.AddTab();
            paragraph.AddText(generatorDocument.DocumentDate);
            // paragraph.AddDateField("dd.MM.yyyy");

            // Create the item table
            this.table = section.AddTable();
            this.table.Style = "Table";
            this.table.Borders.Color = TableBorder;
            this.table.Borders.Width = 0.25;
            this.table.Borders.Left.Width = 0.5;
            this.table.Borders.Right.Width = 0.5;
            this.table.Rows.LeftIndent = 0;

            // Before you can add a row, you must define the columns
            Column column = this.table.AddColumn("1cm"); // Line
            column.Format.Alignment = ParagraphAlignment.Center;

            column = this.table.AddColumn("6cm"); // Description
            column.Format.Alignment = ParagraphAlignment.Left;

            column = this.table.AddColumn("1cm"); // Quantity
            column.Format.Alignment = ParagraphAlignment.Right;

            column = this.table.AddColumn("2cm"); // Unit Price
            column.Format.Alignment = ParagraphAlignment.Right;

            column = this.table.AddColumn("1cm"); // Discount (%)
            column.Format.Alignment = ParagraphAlignment.Right;

            column = this.table.AddColumn("1cm"); // Taxable
            column.Format.Alignment = ParagraphAlignment.Center;

            column = this.table.AddColumn("4cm"); // Extended Price
            column.Format.Alignment = ParagraphAlignment.Right;

         
            var row = table.AddRow();
            row.HeadingFormat = true;
            row.Format.Alignment = ParagraphAlignment.Center;
            row.Format.Font.Bold = true;
            row.Shading.Color = TableBlue;
            row.Cells[0].AddParagraph("");
            row.Cells[0].Format.Alignment = ParagraphAlignment.Left;
            row.Cells[1].AddParagraph("Description");
            row.Cells[1].Format.Alignment = ParagraphAlignment.Left;
            row.Cells[2].AddParagraph("Qty");
            row.Cells[2].Format.Alignment = ParagraphAlignment.Left;
            var s = $"Unit\nPrice";
            row.Cells[3].AddParagraph(s);
            row.Cells[3].Format.Alignment = ParagraphAlignment.Right;
            row.Cells[4].AddParagraph("Disc (%)");
            row.Cells[4].Format.Alignment = ParagraphAlignment.Left;
            row.Cells[5].AddParagraph("Tax");
            row.Cells[5].Format.Alignment = ParagraphAlignment.Left;
            row.Cells[6].AddParagraph("Line Value");
            row.Cells[6].Format.Alignment = ParagraphAlignment.Left;
            this.table.SetEdge(0, 0, 6, 1, Edge.Box, BorderStyle.Single, 0.75, Color.Empty);
        }

        

        /// <summary>
        /// Creates the dynamic parts of the invoice.
        /// </summary>
        void FillContent()
        {
            // Fill address in address text frame
            Paragraph paragraph = this.addressFrame.AddParagraph();
            paragraph.AddText(generatorDocument.SingleName);
            // paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.AddLineBreak();
            paragraph.AddText(generatorDocument.AddressLine1);
            paragraph.AddLineBreak();
            paragraph.AddText(generatorDocument.AddressLine2);
            paragraph.AddLineBreak();
            paragraph.AddText(generatorDocument.PostalCode + " " + generatorDocument.City);
            paragraph.AddLineBreak();
            paragraph.AddText(generatorDocument.Account);

            double totalExtendedPrice = 0;
            foreach (var item in generatorDocument.Lines)
            {
                double quantity = item.Quantity;
                double price = item.Price;
                double discount = item.Discount;
                              
                Row row1 = this.table.AddRow();

                row1.Cells[0].AddParagraph(item.ItemNumber);
                row1.Cells[1].AddParagraph($"{item.Isbn} {item.Title}");
                row1.Cells[2].AddParagraph(quantity.ToString());
                row1.Cells[3].AddParagraph(price.ToString("0.000"));
                row1.Cells[4].AddParagraph(discount.ToString("0.0"));
                row1.Cells[5].AddParagraph("x");
                double extendedPrice = quantity * price;
                extendedPrice = extendedPrice * (100 - discount) / 100;
                row1.Cells[6].AddParagraph(extendedPrice.ToString("0.00"));
                row1.Cells[6].VerticalAlignment = VerticalAlignment.Bottom;
                totalExtendedPrice += extendedPrice;

                this.table.SetEdge(0, this.table.Rows.Count - 2, 6, 2, Edge.Box, BorderStyle.Single, 0.75);
            }

            // Add an invisible row as a space line to the table
            Row row = this.table.AddRow();
            row.Borders.Visible = false;

            // Add the total price row
            row = this.table.AddRow();
            row.Cells[0].Borders.Visible = false;
            row.Cells[0].AddParagraph("Total Price");
            row.Cells[0].Format.Font.Bold = true;
            row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
            row.Cells[0].MergeRight = 5;
            row.Cells[6].AddParagraph(totalExtendedPrice.ToString("0.00"));

            // Add the VAT row
            row = this.table.AddRow();
            row.Cells[0].Borders.Visible = false;
            row.Cells[0].AddParagraph("VAT (19%)");
            row.Cells[0].Format.Font.Bold = true;
            row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
            row.Cells[0].MergeRight = 5;
            row.Cells[6].AddParagraph((0.19 * totalExtendedPrice).ToString("0.00"));

            Random r = new Random();
            double shipping = r.NextDouble() * 37;

            // Add the additional fee row
            row = this.table.AddRow();
            row.Cells[0].Borders.Visible = false;
            row.Cells[0].AddParagraph("Shipping and Handling");
            row.Cells[6].AddParagraph(shipping.ToString("0.00"));
            row.Cells[0].Format.Font.Bold = true;
            row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
            row.Cells[0].MergeRight = 5;

            // Add the total due row
            row = this.table.AddRow();
            row.Cells[0].AddParagraph("Total Due");
            row.Cells[0].Borders.Visible = false;
            row.Cells[0].Format.Font.Bold = true;
            row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
            row.Cells[0].MergeRight = 5;
            totalExtendedPrice += 0.19 * totalExtendedPrice;
            totalExtendedPrice += shipping;
            row.Cells[6].AddParagraph(totalExtendedPrice.ToString("0.00"));

            // Set the borders of the specified cell range
            this.table.SetEdge(5, this.table.Rows.Count - 4, 1, 4, Edge.Box, BorderStyle.Single, 0.75);

            // Add the notes paragraph
            paragraph = this.document.LastSection.AddParagraph();
            paragraph.Format.SpaceBefore = "1cm";
            paragraph.Format.Borders.Width = 0.75;
            paragraph.Format.Borders.Distance = 3;
            paragraph.Format.Borders.Color = TableBorder;
            paragraph.Format.Shading.Color = TableGray;
            if (generatorDocument.Notes != null) paragraph.AddText(generatorDocument.Notes);
        }

       

        /// <summary>
        /// Gets an element value as double from the XML data.
        /// </summary>
       

        // Some pre-defined colors
#if true
        // RGB colors
        readonly static Color TableBorder = new Color(81, 125, 192);
        readonly static Color TableBlue = new Color(235, 240, 249);
        readonly static Color TableGray = new Color(242, 242, 242);
#else
    // CMYK colors
    readonly static Color tableBorder = Color.FromCmyk(100, 50, 0, 30);
    readonly static Color tableBlue = Color.FromCmyk(0, 80, 50, 30);
    readonly static Color tableGray = Color.FromCmyk(30, 0, 0, 0, 100);
#endif
    }
}