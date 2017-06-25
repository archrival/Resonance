using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Resonance.Data.Models
{
    [JsonObject("mediaBundle")]
    public class MediaBundle<T> where T : MediaBase
    {
        public IEnumerable<Disposition> Dispositions { get; set; }
        public T Media { get; set; }
        public IEnumerable<Playback> Playback { get; set; }

        public static MediaBundle<T> FromDynamic(dynamic result, Guid userId)
        {
            var mediaBundle = new MediaBundle<T>();

            var type = typeof(T);

            if (type == typeof(Artist))
            {
                mediaBundle.Media = Artist.FromDynamic(result);
            }
            else if (type == typeof(Album))
            {
                mediaBundle.Media = Album.FromDynamic(result);
            }
            else if (type == typeof(Track))
            {
                mediaBundle.Media = Track.FromDynamic(result);
            }
            else
            {
                return null;
            }

            var disposition = Disposition.FromDynamic(result);
            disposition.MediaId = mediaBundle.Media.Id;
            disposition.UserId = userId;

            var playback = Models.Playback.FromDynamic(result);

            mediaBundle.Dispositions = new List<Disposition> { disposition };

            mediaBundle.Playback = playback != null ? new List<Playback> { playback } : new List<Playback>();

            return mediaBundle;
        }
    }
}