using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GS.Server.Alignment
{
    public class AlignmentPoint : INotifyPropertyChanged
    {
        /// <summary>
        /// Unique ID for this alignment point (optional but useful for UI/debug).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Timestamp when the sync/alignment was performed.
        /// </summary>
        public DateTime AlignTime { get; set; }

        /// <summary>
        /// The ideal mount axis angles corresponding to the true sky position
        /// at the moment of sync (RA/Dec or Alt/Az depending on mount mode).
        /// </summary>
        public AxisPosition Ideal { get; set; }

        /// <summary>
        /// The raw mount axis angles reported by the mount before sync.
        /// </summary>
        public AxisPosition Unsynced { get; set; }

        /// <summary>
        /// Hour angle at the moment of sync (computed by SkyServer).
        /// This must be injected at creation time to avoid time drift.
        /// </summary>
        public double HourAngle { get; set; }

        [JsonIgnore]
        public AxisPositionRad RawRad => Unsynced.ToRad();

        [JsonIgnore]
        public AxisPositionRad IdealRad => Ideal.ToRad();

        /// <summary>
        /// Residual = Unsynced - Ideal (in axis space).
        /// Useful for debugging and model diagnostics.
        /// </summary>
        [JsonIgnore]
        public AxisPosition Residual =>
            new AxisPosition(
                Unsynced.A1 - Ideal.A1,
                Unsynced.A2 - Ideal.A2
            );

        public AlignmentPoint() { }

        public AlignmentPoint(AxisPosition ideal, AxisPosition unsynced, double hourAngle, DateTime time)
        {
            Ideal = ideal;
            Unsynced = unsynced;
            HourAngle = hourAngle;
            AlignTime = time;
        }

        #region INotifyPropertyChanged interface ...
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

    }
    public class AlignmentPointCollection : ObservableCollection<AlignmentPoint>
    {
        protected override void InsertItem(int index, AlignmentPoint item)
        {
            if (item != null && item.Id == 0)
            {
                if (Items.Any())
                {
                    item.Id = Items.Max(i => i.Id) + 1;
                }
                else
                {
                    item.Id = 1;
                }
            }
            base.InsertItem(index, item);
        }


    }

}
