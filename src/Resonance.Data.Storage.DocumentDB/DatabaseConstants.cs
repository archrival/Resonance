using System.Collections.Generic;

namespace Resonance.Data.Storage.DocumentDB
{
    public static class DatabaseConstants
    {
        public const string Album = nameof(Album);
        public const string Artist = nameof(Artist);
        public const string Chat = nameof(Chat);
        public const string Collection = nameof(Collection);
        public const string DatabaseId = "Resonance";
        public const string Configuration = nameof(Configuration);
        public const string Disposition = nameof(Disposition);
        public const string Genre = nameof(Genre);
        public const string Marker = nameof(Marker);
        public const string MediaInfo = nameof(MediaInfo);
        public const string Playback = nameof(Playback);
        public const string Playlist = nameof(Playlist);
        public const string Role = nameof(Role);
        public const string Track = nameof(Track);
        public const string User = nameof(User);

        public static readonly List<string> AllDatabases = new List<string>
        {
            Album,
            Artist,
            Chat,
            Collection,
            Configuration,
            Disposition,
            Genre,
            Marker,
            MediaInfo,
            Playback,
            Playlist,
            Role,
            Track,
            User
        };
    }
}