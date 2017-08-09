using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Storage;

namespace robot.sl.Audio.AudioPlaying
{
    public class AudioPlayer
    {
        private AudioGraph _graph;
        private Dictionary<string, AudioFileInputNode> _fileInputs = new Dictionary<string, AudioFileInputNode>();
        private AudioDeviceOutputNode _deviceOutput;
        private const string HEADSET_NAME = "logitech";

        public async Task Initialize(bool isHeadset)
        {
            await CreateAudioGraphAsync(isHeadset);
        }

        public async Task AddFileAsync(IStorageFile file)
        {
            var fileInputNodeResult = await _graph.CreateFileInputNodeAsync(file);
            var fileInputNode = fileInputNodeResult.FileInputNode;
            fileInputNode.Stop();
            fileInputNode.AddOutgoingConnection(_deviceOutput);
            _fileInputs.Add(file.Name, fileInputNode);
        }

        public async Task Play(string key, double gain, CancellationToken? cancellationToken)
        {
            foreach (var soundeNode in _fileInputs.Select(fi => fi.Value))
            {
                soundeNode.Stop();
            }
            
            var sound = _fileInputs[key];
            sound.OutgoingGain = gain;
            sound.Seek(TimeSpan.Zero);
            sound.Start();

            if(cancellationToken.HasValue == false)
                await Task.Delay(sound.Duration);
            else
                await Task.Delay(sound.Duration, cancellationToken.Value);
        }

        private async Task CreateAudioGraphAsync(bool isHeadset)
        {
            var settings = new AudioGraphSettings(AudioRenderCategory.Communications);
            settings.EncodingProperties = AudioEncodingProperties.CreatePcm(96000, 1, 24);
            var devices = await DeviceInformation.FindAllAsync(MediaDevice.GetAudioRenderSelector());
            settings.PrimaryRenderDevice = devices.First(d => (isHeadset ? (d.Name ?? string.Empty).ToLower().Contains(HEADSET_NAME) : !(d.Name ?? string.Empty).ToLower().Contains(HEADSET_NAME)));
            var result = await AudioGraph.CreateAsync(settings);
            _graph = result.Graph;
            var deviceOutputNodeResult = await _graph.CreateDeviceOutputNodeAsync();
            _deviceOutput = deviceOutputNodeResult.DeviceOutputNode;
            _graph.ResetAllNodes();
            _graph.Start();
        }
    }
}
