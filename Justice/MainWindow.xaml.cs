using Microsoft.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace Justice
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string? connectionString = "";
        private const string initQueryText = @"<Section xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xml:space=""preserve"" TextAlignment=""Left"" LineHeight=""Auto"" IsHyphenationEnabled=""False"" xml:lang=""en-us"" FlowDirection=""LeftToRight"" NumberSubstitution.CultureSource=""User"" NumberSubstitution.Substitution=""AsCulture"" FontFamily=""Segoe UI"" FontStyle=""Normal"" FontWeight=""Normal"" FontStretch=""Normal"" FontSize=""12"" Foreground=""#FF000000"" Typography.StandardLigatures=""True"" Typography.ContextualLigatures=""True"" Typography.DiscretionaryLigatures=""False"" Typography.HistoricalLigatures=""False"" Typography.AnnotationAlternates=""0"" Typography.ContextualAlternates=""True"" Typography.HistoricalForms=""False"" Typography.Kerning=""True"" Typography.CapitalSpacing=""False"" Typography.CaseSensitiveForms=""False"" Typography.StylisticSet1=""False"" Typography.StylisticSet2=""False"" Typography.StylisticSet3=""False"" Typography.StylisticSet4=""False"" Typography.StylisticSet5=""False"" Typography.StylisticSet6=""False"" Typography.StylisticSet7=""False"" Typography.StylisticSet8=""False"" Typography.StylisticSet9=""False"" Typography.StylisticSet10=""False"" Typography.StylisticSet11=""False"" Typography.StylisticSet12=""False"" Typography.StylisticSet13=""False"" Typography.StylisticSet14=""False"" Typography.StylisticSet15=""False"" Typography.StylisticSet16=""False"" Typography.StylisticSet17=""False"" Typography.StylisticSet18=""False"" Typography.StylisticSet19=""False"" Typography.StylisticSet20=""False"" Typography.Fraction=""Normal"" Typography.SlashedZero=""False"" Typography.MathematicalGreek=""False"" Typography.EastAsianExpertForms=""False"" Typography.Variants=""Normal"" Typography.Capitals=""Normal"" Typography.NumeralStyle=""Normal"" Typography.NumeralAlignment=""Normal"" Typography.EastAsianWidths=""Normal"" Typography.EastAsianLanguage=""Normal"" Typography.StandardSwashes=""0"" Typography.ContextualSwashes=""0"" Typography.StylisticAlternates=""0""><Paragraph FontFamily=""Consolas"" FontSize=""12.666666666666666"" Margin=""0,0,0,0""><Span Foreground=""#FF0000FF""><Run>DECLARE</Run></Span><Span><Run> @RC </Run></Span><Span Foreground=""#FF0000FF""><Run>int</Run></Span></Paragraph><Paragraph FontFamily=""Consolas"" FontSize=""12.666666666666666"" Margin=""0,0,0,0""><Span Foreground=""#FF0000FF""><Run>DECLARE</Run></Span><Span><Run> @@Program </Run></Span><Span Foreground=""#FF0000FF""><Run>varchar</Run></Span><Span Foreground=""#FF808080""><Run>(</Run></Span><Span><Run>4</Run></Span><Span Foreground=""#FF808080""><Run>)</Run></Span></Paragraph><Paragraph FontFamily=""Consolas"" FontSize=""12.666666666666666"" Margin=""0,0,0,0""><Span Foreground=""#FF0000FF""><Run>DECLARE</Run></Span><Span><Run> @@ProgramVersion </Run></Span><Span Foreground=""#FF0000FF""><Run>varchar</Run></Span><Span Foreground=""#FF808080""><Run>(</Run></Span><Span><Run>20</Run></Span><Span Foreground=""#FF808080""><Run>)</Run></Span></Paragraph><Paragraph FontFamily=""Consolas"" FontSize=""12.666666666666666"" Margin=""0,0,0,0""></Paragraph><Paragraph FontFamily=""Consolas"" FontSize=""12.666666666666666"" Margin=""0,0,0,0""><Span Foreground=""#FF008000""><Run>-- TODO: Set parameter values here.</Run></Span></Paragraph><Paragraph FontFamily=""Consolas"" FontSize=""12.666666666666666"" Margin=""0,0,0,0""></Paragraph><Paragraph FontFamily=""Consolas"" FontSize=""12.666666666666666"" Margin=""0,0,0,0""><Span Foreground=""#FF0000FF""><Run>EXECUTE</Run></Span><Span><Run> @RC </Run></Span><Span Foreground=""#FF808080""><Run>=</Run></Span><Span><Run> [dbo]</Run></Span><Span Foreground=""#FF808080""><Run>.</Run></Span><Span><Run>[Proc_Version]</Run></Span><Span Foreground=""#FF0000FF""><Run> </Run></Span></Paragraph><Paragraph FontFamily=""Consolas"" FontSize=""12.666666666666666"" Margin=""0,0,0,0""><Span Foreground=""#FF0000FF""><Run>   </Run></Span><Span><Run>@@Program</Run></Span></Paragraph><Paragraph FontFamily=""Consolas"" FontSize=""12.666666666666666"" Margin=""0,0,0,0""><Span><Run>  </Run></Span><Span Foreground=""#FF808080""><Run>,</Run></Span><Span><Run>@@ProgramVersion</Run></Span></Paragraph><Paragraph FontFamily=""Consolas"" FontSize=""12.666666666666666"" Margin=""0,0,0,0""><Span Foreground=""#FF0000FF""></Span></Paragraph><Paragraph><Run></Run></Paragraph></Section>";
        private const string outputDefault = "public sealed class DefaultOutput\n{\n\n}";
        public MainWindow()
        {
            InitializeComponent();
            SetXaml(QueryRichTextBox, initQueryText);
            OutputTextBox.Text = outputDefault;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConnectButton.IsEnabled = false;
                ConnectedStatusTextBlock.Text = "Connecting...";
                ConnectedStatusTextBlock.Background = Brushes.Yellow;
                var constrbuild = new SqlConnectionStringBuilder();
                constrbuild.ApplicationName = "Generate CSharp POCO";
                constrbuild.InitialCatalog = DatabaseNameTextBox.Text;
                constrbuild.IntegratedSecurity = true;
                constrbuild.TrustServerCertificate = true;
                constrbuild.DataSource = ServerNameTextBox.Text;
                using var client = new SqlConnection(constrbuild.ConnectionString);
                await client.OpenAsync();
                await client.CloseAsync();
                ConnectedStatusTextBlock.Text = "Connected";
                ConnectedStatusTextBlock.Background = Brushes.Green;
                connectionString = constrbuild.ConnectionString;
                ExecuteButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                //todo:logger
                ConnectedStatusTextBlock.Text = ex.Message;
                ConnectedStatusTextBlock.Background = Brushes.Red;
            }
            finally
            {
                ConnectButton.IsEnabled = true;
            }


        }

        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionString)) return;
                ExecuteButton.IsEnabled = false;
                var exampletext = StringFromRichTextBox(QueryRichTextBox);
                var x = await GetSqlInfo.GetSqlProfile(connectionString, exampletext, StoredProcNameTextBox.Text);
                var y = await ClassGenerator.GenerateClass(x);
                OutputTextBox.Text = y;
            }
            catch (Exception ex)
            {
                ConnectedStatusTextBlock.Text = ex.Message;
                ConnectedStatusTextBlock.Background = Brushes.Red;
            }
            finally
            {
                ExecuteButton.IsEnabled = true;
            }
        }
        static string GetXaml(RichTextBox rt)
        {
            TextRange range = new TextRange(rt.Document.ContentStart, rt.Document.ContentEnd);
            MemoryStream stream = new MemoryStream();
            range.Save(stream, DataFormats.Xaml);
            string xamlText = Encoding.UTF8.GetString(stream.ToArray());
            return xamlText;
        }
        static void SetXaml(RichTextBox rt, string xamlString)
        {
            StringReader stringReader = new StringReader(xamlString);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            Section sec = XamlReader.Load(xmlReader) as Section;
            FlowDocument doc = new FlowDocument();
            while (sec.Blocks.Count > 0)
                doc.Blocks.Add(sec.Blocks.FirstBlock);
            rt.Document = doc;
        }
        string StringFromRichTextBox(RichTextBox rtb)
        {
            //https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-extract-the-text-content-from-a-richtextbox?view=netframeworkdesktop-4.8
            TextRange textRange = new TextRange(
                // TextPointer to the start of content in the RichTextBox.
                rtb.Document.ContentStart,
                // TextPointer to the end of content in the RichTextBox.
                rtb.Document.ContentEnd
            );

            // The Text property on a TextRange object returns a string
            // representing the plain text content of the TextRange.
            return textRange.Text;
        }

        private void ExecuteButton2_Click(object sender, RoutedEventArgs e)
        {
            var x = GetXaml(QueryRichTextBox);

        }
    }
}