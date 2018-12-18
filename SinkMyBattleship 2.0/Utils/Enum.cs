using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinkMyBattleship_2._0.Utils
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum GenericEnum)
        {
            var genericEnumType = GenericEnum.GetType();
            var memberInfo = genericEnumType.GetMember(GenericEnum.ToString());
            if ((memberInfo != null && memberInfo.Length > 0))
            {
                var _Attribs = memberInfo[0].GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
                if ((_Attribs != null && _Attribs.Count() > 0))
                {
                    return ((System.ComponentModel.DescriptionAttribute)_Attribs.ElementAt(0)).Description;
                }
            }
            return GenericEnum.ToString();
        }
    }
    public enum StatusCode
    {
        [Description("210 BATTLESHIP/1.0")]
        Battleship = 210,
        [Description("221 Client Starts")]
        ClientStart = 221,
        [Description("222 Host Starts")]
        HostStart = 222,
        [Description("230 Miss!")]
        Miss = 230,
        [Description("241 You hit my Carrier")]
        CarrierHit = 241,
        [Description("242 You hit my Battleship")]
        BattleshipHit = 242,
        [Description("243 You hit my Destroyer")]
        DestroyerHit = 243,
        [Description("244 You hit my Submarine")]
        SubmarineHit = 244,
        [Description("245 You hit my Patrol Boat")]
        PatrolBoatHit = 245,
        [Description("251 You sunk my Carrier")]
        CarrierSunk = 251,
        [Description("252 You sunk my Battleship")]
        BattleshipSunk = 252,
        [Description("253 You sunk my Destroyer")]
        DestroyerSunk = 253,
        [Description("254 You sunk my Submarine")]
        SubmarineSunk = 254,
        [Description("255 You sunk my Patrol Boat")]
        PatrolBoatSunk = 255,
        [Description("260 You Win!")]
        YouWin = 260,
        [Description("270 The other player chickened out")]
        ConnectionLost = 270,
        [Description("500 Syntax Error")]
        SyntaxError = 500,
        [Description("501 Sequence Error")]
        SequenceError = 501
    }
}
