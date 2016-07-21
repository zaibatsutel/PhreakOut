using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PhreakOut
{

    // We are initializing a COM interface for use within the namespace
    // This interface allows access to memory at the byte level which we need to populate audio data that is generated
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]

    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ToneSampleGenerator mToneSampleGen = new ToneSampleGenerator();


        private AudioGraph graph;
        private AudioDeviceOutputNode deviceOutputNode;
        private AudioFrameInputNode frameInputNode;

        public MainPage()
        {
            this.InitializeComponent();

            mToneSampleGen.Volume = 0.5;

            // hook up all the things
            hookupEvents(auxButtons);
            hookupEvents(keypadbuttons);


        }

        private void hookupEvents(Grid g)
        {
            foreach(var child in g.Children)
            {
                // hook up a thing.
                if(child is Button)
                {
                    Button b = child as Button;
                    b.AddHandler(PointerPressedEvent, new PointerEventHandler(startTone), true);
                    b.AddHandler(PointerReleasedEvent, new PointerEventHandler(stopToneGenEV), true);
                }
            }
        }

       private void startTone(object sender, PointerRoutedEventArgs args)
        {
            // Check if it's not a button (It might be!)
            // if it is a button, check that the tag isn't null or isn't a string.

            if(sender is Button == false)
            {
                System.Diagnostics.Debug.WriteLine("-> sender wasn't a button");
                return;
            }
            if((sender as Button).Tag == null)
            {
                System.Diagnostics.Debug.WriteLine("-> sender tag was null");
                return;
            }
            if((sender as Button).Tag as string == null)
            {
                System.Diagnostics.Debug.WriteLine("-> button tag was not string");
                return;
            }


            String tag = (String)(((Button)(sender)).Tag);

            // stop the pipe to make sure there's nothing wrong

            frameInputNode.Stop();

            string[] tonesStrings = tag.Split(',');
            int[] tTones = new int[tonesStrings.Length];
            for (int i = 0; i < tonesStrings.Length; i++)
                tTones[i] = int.Parse(tonesStrings[i]);
            mToneSampleGen.Tones = tTones;

            frameInputNode.Start();

        }

        private void stopToneGenEV(object sender, PointerRoutedEventArgs e)
        {
            frameInputNode.Stop();

            mToneSampleGen.Tones = new int[] { 0 };
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Spin up the audio graph
            await CreateAudioGraph();
        }


        private async Task CreateAudioGraph()
        {
            // Create an AudioGraph with default settings
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Media);
            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                // Cannot create graph
                // Say something about it.
                System.Diagnostics.Debug.WriteLine("Couldn't create audiograph!");
                return;
            }

            graph = result.Graph;

            // Create a device output node
            CreateAudioDeviceOutputNodeResult deviceOutputNodeResult = await graph.CreateDeviceOutputNodeAsync();
            if (deviceOutputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                // Cannot create device output node
                // Say something about that
                System.Diagnostics.Debug.WriteLine("Unable to generate output node!");
                return;
            }

            deviceOutputNode = deviceOutputNodeResult.DeviceOutputNode;

            // Create the FrameInputNode at the same format as the graph, except explicitly set mono.
            AudioEncodingProperties nodeEncodingProperties = graph.EncodingProperties;
            nodeEncodingProperties.ChannelCount = 1;
            frameInputNode = graph.CreateFrameInputNode(nodeEncodingProperties);
            frameInputNode.AddOutgoingConnection(deviceOutputNode);

            System.Diagnostics.Debug.WriteLine("Starting graph at " + (frameInputNode.EncodingProperties.SampleRate) + " samplerate");
            mToneSampleGen.SampleRate = frameInputNode.EncodingProperties.SampleRate;

            // Initialize the Frame Input Node in the stopped state
            frameInputNode.Stop();

            // Hook up an event handler so we can start generating samples when needed
            // This event is triggered when the node is required to provide data
            frameInputNode.QuantumStarted += node_QuantumStarted;

            System.Diagnostics.Debug.WriteLine("Starting graph");
            // Start the graph since we will only start/stop the frame input node
            graph.Start();
        }


        unsafe private AudioFrame GenerateAudioData(uint samples)
        {
            // Buffer size is (number of samples) * (size of each sample)
            // We choose to generate single channel (mono) audio. For multi-channel, multiply by number of channels
            uint bufferSize = samples * sizeof(float);
            AudioFrame frame = new Windows.Media.AudioFrame(bufferSize);

            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;
                float* dataInFloat;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                // Cast to float since the data we are generating is float
                dataInFloat = (float*)dataInBytes;

                float[] tonesGenerated = mToneSampleGen.getSamples(samples);

                // Since there's no good way of doing this, we're going to copy the whole thing over
                // this means that there's some slowdown, yo
                for (int i = 0; i < samples; i++)
                {
                    dataInFloat[i] = tonesGenerated[i];
                }
            }

            return frame;
        }


        private void node_QuantumStarted(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args)
        {
            if(args.RequiredSamples > 0)
            {
                frameInputNode.AddFrame(GenerateAudioData((uint)(args.RequiredSamples)));
            }
        }

    }
}
