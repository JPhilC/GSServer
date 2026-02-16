/*
BSD 2-Clause License

Copyright (c) 2019, LunaticSoftware.org, Email: phil@lunaticsoftware.org
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
*/

using System;
using GS.Principles;
using Newtonsoft.Json;

namespace GS.Server.Alignment
{
    /// <summary>
    /// A structure to represent telescope mount axis positions
    /// </summary>
    public struct AxisPosition
    {
        private double _a1;
        private double _a2;

        [JsonProperty]
        public double A1
        {
            get => _a1;
            set => _a1 = value;
        }

        [JsonProperty]
        public double A2
        {
            get => _a2;
            set => _a2 = value;
        }


        public AxisPosition(double[] axes) : this(axes[0], axes[1])
        { }

        /// <summary>
        /// Create a new axis position
        /// </summary>
        /// <param name="ra">RA axis encoder value</param>
        /// <param name="dec">Dec axis encoder value</param>
        public AxisPosition(double ra, double dec)
        {
            _a1 = ra;
            _a2 = dec;
        }


        public AxisPosition(string axisPositions)
        {
            var positions = axisPositions.Split('|');
            try
            {
                _a1 = double.Parse(positions[0]);
                _a2 = double.Parse(positions[1]);
            }
            catch
            {
                throw new ArgumentException("Badly formed axis position string");
            }
        }


        public static implicit operator double[](AxisPosition axes)
        {
            return new[] { axes[0], axes[1] };
        }

        public static implicit operator AxisPosition(double[] axes)
        {
            return new AxisPosition(axes);
        }

        /// <summary>
        /// The axis position in degrees
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public double this[int index]
        {
            get
            {
                if (index < 0 || index > 2)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return (index == 0 ? _a1 : _a2);
            }
            set
            {
                if (index < 0 || index > 2)
                {
                    throw new ArgumentOutOfRangeException();
                }
                if (index == 0)
                {
                    _a1 = value;
                }
                else
                {
                    _a2 = value;
                }
            }
        }

        /// <summary>
        /// Compares the two specified sets of Axis positions.
        /// </summary>
        public static bool operator ==(AxisPosition pos1, AxisPosition pos2)
        {
            return (pos1.A1 == pos2.A1 && pos1.A2 == pos2.A2);
        }

        public static bool operator !=(AxisPosition pos1, AxisPosition pos2)
        {
            return !(pos1 == pos2);
        }

        public static AxisPosition operator -(AxisPosition pos1, AxisPosition pos2)
        {
            return new AxisPosition(pos1.A1 - pos2.A1, pos1.A2 - pos2.A2);
        }

        public static AxisPosition operator +(AxisPosition pos1, AxisPosition pos2)
        {
            return new AxisPosition(pos1.A1 + pos2.A1, pos1.A2 + pos2.A2);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + _a1.GetHashCode();
                hash = hash * 23 + _a2.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            return (obj is AxisPosition position
                    && this == position);
        }


        public bool Equals(AxisPosition obj, double toleranceDegrees)
        {
            var deltaRa = Math.Abs(obj.A1 - A1);
            deltaRa = (deltaRa + 180) % 360 - 180;
            var deltaDec = Math.Abs(obj.A2 - A2);
            deltaDec = (deltaDec + 180) % 360 - 180;
            return (deltaRa <= toleranceDegrees
               && deltaDec <= toleranceDegrees);
        }


        public double IncludedAngleTo(AxisPosition axisPosition2)
        {
            double piby2 = Math.PI * 0.5;
            double thisRARadians = Units.Deg2Rad(this.A1);
            double thisDecRadians = Units.Deg2Rad(this.A2);
            double thatRARadians = Units.Deg2Rad(axisPosition2.A1);
            double thatDecRadians = Units.Deg2Rad(axisPosition2.A2);
            // Using the law of Cosines
            double c = (Math.Cos(piby2 - thisDecRadians) * Math.Cos(piby2 - thatDecRadians))
                       + (Math.Sin(piby2 - thisDecRadians) * Math.Sin(piby2 - thatDecRadians) *
                          Math.Cos(thisRARadians - thatRARadians));
            double angleRadians = Math.Abs(Math.Acos(c));
            return Units.Rad2Deg(angleRadians);

        }

    }

    public readonly struct AxisPositionRad
    {
        public double A1 { get; }
        public double A2 { get; }

        public AxisPositionRad(double a1, double a2)
        {
            A1 = a1;
            A2 = a2;
        }
    }

    public static class AxisPositionExtensions
    {
        public static AxisPositionRad ToRad(this AxisPosition p) =>
            new AxisPositionRad(
                Units.Deg2Rad(p.A1),
                Units.Deg2Rad(p.A2)
            );

        public static AxisPosition FromRad(this AxisPositionRad p) =>
            new AxisPosition(
                Units.Rad2Deg(p.A1),
                Units.Rad2Deg(p.A2)
            );
    }


}
