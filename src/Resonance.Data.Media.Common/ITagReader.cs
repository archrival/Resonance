using Resonance.Common;
using Resonance.Data.Models;
using System;
using System.Collections.Generic;

namespace Resonance.Data.Media.Common
{
    public interface ITagReader
    {
        string[] AlbumArtists { get; }

        string AlbumName { get; }

        string[] Artists { get; }

        int Bitrate { get; }

        int Channels { get; }

        string Comment { get; }

        string[] Composers { get; }

        string ContentType { get; }

        IEnumerable<CoverArt> CoverArt { get; set; }

        DateTime DateCreated { get; }

        DateTime DateModified { get; }

        uint DiscNumber { get; }

        TimeSpan Duration { get; }

        string[] Genres { get; }

        string Hash { get; }

        HashType HashType { get; }

        string MusicBrainzAlbumId { get; }

        string MusicBrainzArtistId { get; }

        string MusicBrainzTrackId { get; }

        string Path { get; }

        uint ReleaseDate { get; }

        double ReplayGainAlbumGain { get; }

        double ReplayGainAlbumPeak { get; }

        double ReplayGainTrackGain { get; }

        double ReplayGainTrackPeak { get; }

        int SampleRate { get; }

        long Size { get; }

        string TrackName { get; }

        uint TrackNumber { get; }

        void ReadTag(string path, HashType hashType = HashType.None);
    }
}