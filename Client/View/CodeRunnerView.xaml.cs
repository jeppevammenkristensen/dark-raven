using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Client.ViewModels;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace Client.View
{
    /// <summary>
    /// Interaction logic for CodeRunnerView.xaml
    /// </summary>
    public partial class CodeRunnerView : UserControl
    {
        public CodeRunnerView()
        {
            InitializeComponent();
            TextEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
            TextEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
        }

        ToolTip toolTip = new ToolTip();


        private void TextEditor_OnMouseHover(object sender, MouseEventArgs e)
        {
            var pos = TextEditor.GetPositionFromPoint(e.GetPosition(TextEditor));
            if (pos != null && DataContext is CodeRunnerViewModel codeRunner)
            {
                var word = codeRunner.GetWord(pos);

                toolTip.PlacementTarget = this; // required for property inheritance
                toolTip.Content = word;
                toolTip.IsOpen = true;
                e.Handled = true;
            }
        }

        private void TextEditor_OnMouseHoverStopped(object sender, MouseEventArgs e)
        {
            toolTip.IsOpen = false;
        }

        CompletionWindow completionWindow;

        async void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == "." && DataContext is CodeRunnerViewModel viewModel) {
                
                // Open code completion after the user has pressed dot:
                completionWindow = new CompletionWindow(TextEditor.TextArea);
                //var location = TextEditor.Document.GetLocation(TextEditor.CaretOffset);
                var result = await viewModel.GetSuggestions(TextEditor.CaretOffset);

                IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                foreach (var sug in result)
                {
                    data.Add(new MyCompletionData(sug));
                }

                completionWindow.Show();
                completionWindow.Closed += delegate {
                    completionWindow = null;
                };
            }
        }

        void textEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && completionWindow != null) {
                if (!char.IsLetterOrDigit(e.Text[0])) {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    completionWindow.CompletionList.RequestInsertion(e);
                }
            }
            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }
    }

    public class MyCompletionData : ICompletionData
    {
        public MyCompletionData(string text)
        {
            this.Text = text;
        }

        public System.Windows.Media.ImageSource Image {
            get { return null; }
        }

        public string Text { get; private set; }

        // Use this property if you want to show a fancy UIElement in the list.
        public object Content {
            get { return this.Text; }
        }

        public object Description {
            get { return "Description for " + this.Text; }
        }

        public double Priority { get; }

        public void Complete(TextArea textArea, ISegment completionSegment,
            EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, this.Text);
        }
    }
}
