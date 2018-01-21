using robot.sl.CarControl;
using robot.sl.Helper;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;

namespace robot.sl.Audio
{
    /// <summary>
    /// Microphone: Logitech G933 Headset
    /// </summary>
    public partial class SpeechRecognition
    {
        private SpeechRecognizer _speechRecognizer;
        private volatile bool _isStopped;

        //Dependency objects
        private MotorController _motorController;
        private ServoController _servoController;
        private AutomaticDrive _automaticDrive;

        private ManualResetEvent _threadWaiter = new ManualResetEvent(false);

        public async Task InitialzeAsync(MotorController motorController,
                                    ServoController servoController,
                                    AutomaticDrive automaticDrive)
        {
            _motorController = motorController;
            _servoController = servoController;
            _automaticDrive = automaticDrive;

            _speechRecognizer = new SpeechRecognizer(new Language("de-DE"));

            var grammerFile = await Package.Current.InstalledLocation.GetFileAsync(@"Audio\SpeechRecognizerGrammer.xml");

            var grammarConstraint = new SpeechRecognitionGrammarFileConstraint(grammerFile);
            _speechRecognizer.Constraints.Add(grammarConstraint);
            var compilationResult = await _speechRecognizer.CompileConstraintsAsync();

            _speechRecognizer.ContinuousRecognitionSession.ResultGenerated += RecognationResult;
            _speechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
        }

        public void Start()
        {
            Task.Factory.StartNew(() =>
            {

                _speechRecognizer.ContinuousRecognitionSession.StartAsync().AsTask().Wait();
                _threadWaiter.WaitOne();

            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
             .AsAsyncAction()
             .AsTask()
             .ContinueWith((t) =>
             {

                 Logger.WriteAsync(nameof(SpeechRecognition), t.Exception).Wait();
                 SystemController.ShutdownApplicationAsync(true).Wait();

             }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public async Task StopAsync()
        {
            StopInternal();
            await _speechRecognizer.ContinuousRecognitionSession.StopAsync();
            _threadWaiter.Set();
        }

        private void StopInternal()
        {
            _isStopped = true;

            _motorController.MoveCar(null, new CarMoveCommand
            {
                Speed = 0
            });

            _recognationForwardBackward = true;
            _recognationIsDriving = false;
            _recognationShouldDancing = false;
        }

        private async void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession speechContinousRecognationSession, SpeechContinuousRecognitionCompletedEventArgs speechContinuousRecognationCompletedEventArgs)
        {
            if (_isStopped)
            {
                return;
            }

            await Logger.WriteAsync($"SpeechRecognizer ContinousRecognationSession completed {speechContinuousRecognationCompletedEventArgs.Status}");
            await _speechRecognizer.ContinuousRecognitionSession.StartAsync();
        }
    }
}
