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

        CompletionWindow? completionWindow;

        async void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
           if (e.Text == "." && DataContext is CodeRunnerViewModel viewModel) {
                
                // Open code completion after the user has pressed dot:
                completionWindow = new CompletionWindow(TextEditor.TextArea);
                //var location = TextEditor.Document.GetLocation(TextEditor.CaretOffset);
                var result = await viewModel.GetSuggestions(TextEditor.CaretOffset,false);
           
                IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                foreach (var sug in result)
                {
                    data.Add(sug);
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

        private async void CommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (DataContext is not CodeRunnerViewModel viewModel)
                return;
            
            // Open code completion after the user has pressed dot:
            completionWindow = new CompletionWindow(TextEditor.TextArea);
            //var location = TextEditor.Document.GetLocation(TextEditor.CaretOffset);
            var result = await viewModel.GetSuggestions(TextEditor.CaretOffset, true);

            IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
            
            foreach (var sug in result)
            {
                data.Add(sug);
            }

            if (data.Any())
            {
                completionWindow.Show();
                completionWindow.Closed += delegate {
                    completionWindow = null;
                };    
            }
            
        }
    }

    public class MyCompletionData : ICompletionData
    {
        public string Group { get; set; }

        public MyCompletionData(string text, string display, string group)
        {
            Group = group;
            this.Text = text;
            this.Content = display;
        }

        public System.Windows.Media.ImageSource Image {
            get {
                try
                {
                    return new BitmapImage(new Uri($"pack://application:,,,/Images/{Group}.png"));
                }
                catch 
                {
                    return null;
                }
            }
        }

        public string Text { get; private set; }

        // Use this property if you want to show a fancy UIElement in the list.
        public object Content
        {
            get;
            set;
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
