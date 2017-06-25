using Resonance.Common;
using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TagLib;

namespace Resonance.Data.Media.Tag
{
    public class TagLibTagReader : ITagReader
    {
        private readonly bool _useUtc;
        private FileInfo _fileInfo;
        private TagLib.File _fileTag;
        private string _hash;
        private HashType _hashType;
        private string _path;

        public TagLibTagReader()
        {
            _useUtc = true;
        }

        public TagLibTagReader(bool useUtc = true)
        {
            _useUtc = useUtc;
        }

        public string[] AlbumArtists => _fileTag.Tag.AlbumArtists;

        public string AlbumName => _fileTag.Tag.Album;

        public string[] Artists => _fileTag.Tag.Performers;

        public int Bitrate => _fileTag.Properties.AudioBitrate;

        public int Channels => _fileTag.Properties.AudioChannels;

        public string Comment => _fileTag.Tag.Comment;

        public string[] Composers => _fileTag.Tag.Composers;

        public string ContentType => _fileTag.Properties.Description;

        public IEnumerable<CoverArt> CoverArt { get; set; }

        public DateTime DateCreated => _useUtc ? _fileInfo.CreationTimeUtc : _fileInfo.CreationTime;

        public DateTime DateModified => _useUtc ? _fileInfo.LastWriteTimeUtc : _fileInfo.LastWriteTime;

        public uint DiscNumber => _fileTag.Tag.Disc;

        public TimeSpan Duration => _fileTag.Properties.Duration;

        public string[] Genres => _fileTag.Tag.Genres;

        public string Hash => _hash;

        public HashType HashType => _hashType;

        public string MusicBrainzAlbumId => _fileTag.Tag.MusicBrainzReleaseId;

        public string MusicBrainzArtistId => _fileTag.Tag.MusicBrainzArtistId;

        public string MusicBrainzTrackId => _fileTag.Tag.MusicBrainzTrackId;

        public string Path => _path;

        public uint ReleaseDate => _fileTag.Tag.Year;

        public double ReplayGainAlbumGain => _fileTag.Tag.ReplayGainAlbumGain;

        public double ReplayGainAlbumPeak => _fileTag.Tag.ReplayGainAlbumPeak;

        public double ReplayGainTrackGain => _fileTag.Tag.ReplayGainTrackGain;

        public double ReplayGainTrackPeak => _fileTag.Tag.ReplayGainTrackPeak;

        public int SampleRate => _fileTag.Properties.AudioSampleRate;

        public long Size => _fileInfo.Length;

        public string TrackName => _fileTag.Tag.Title;

        public uint TrackNumber => _fileTag.Tag.Track;

        public void ReadTag(string path, HashType hashType = HashType.None)
        {
            _path = path;
            _hashType = hashType;
            _fileTag = TagLib.File.Create(_path);
            _fileInfo = new FileInfo(_path);

            ReadMediaProperties();
            ReadCoverArt();

            if (hashType != HashType.None)
            {
                _hash = _fileInfo.GetHash(_hashType);
            }
        }

        private void ReadCoverArt()
        {
            var coverArtList = new List<CoverArt>();

            foreach (var picture in _fileTag.Tag.Pictures)
            {
                var coverArtType = CoverArtType.Other;

                switch (picture.Type)
                {
                    case PictureType.Artist:
                        coverArtType = CoverArtType.Artist;
                        break;

                    case PictureType.BackCover:
                        coverArtType = CoverArtType.Back;
                        break;

                    case PictureType.Band:
                        coverArtType = CoverArtType.Band;
                        break;

                    case PictureType.BandLogo:
                        coverArtType = CoverArtType.BandLogo;
                        break;

                    case PictureType.FrontCover:
                        coverArtType = CoverArtType.Front;
                        break;

                    case PictureType.LeadArtist:
                        coverArtType = CoverArtType.LeadArtist;
                        break;

                    case PictureType.LeafletPage:
                        coverArtType = CoverArtType.Leaflet;
                        break;

                    case PictureType.Media:
                        coverArtType = CoverArtType.Media;
                        break;
                }

                var coverArt = new CoverArt
                {
                    MimeType = picture.MimeType,
                    CoverArtType = coverArtType,
                    CoverArtData = picture.Data.Data,
                    Size = picture.Data.Data.Length
                };

                coverArtList.Add(coverArt);
            }

            CoverArt = coverArtList;
        }

        private void ReadMediaProperties()
        {
            var codec = _fileTag.Properties.Codecs.FirstOrDefault();

            var duration = _fileTag.Properties.Duration;

            if (!(codec is TagLib.Mpeg.AudioHeader))
                return;

            var mpegAudioHeader = (TagLib.Mpeg.AudioHeader)codec;

            // Read VBR header to get accurate audio info
            var vbriHeader = mpegAudioHeader.VBRIHeader;
            var xingHeader = mpegAudioHeader.XingHeader;

            // Read Duration first so the bitrate calculation works
            duration = mpegAudioHeader.Duration;
        }
    }
}