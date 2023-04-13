using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CollabLobbyUI
{
    static class CollabLobbyUIUtils
    {
        public static readonly MTexture Gui_Arrow = GFX.Gui["dotarrow_outline"];
        public static readonly MTexture Gui_Cross = GFX.Gui["x"];

        public static bool GetClampedScreenPos(Entity e, Level l, out Vector2 pos)
        {
            if (e == null || l == null)
            {
                pos = Vector2.Zero;
                return false;
            }
            Vector2 posScreen = l.WorldToScreen(e.Center);
            pos = posScreen.Clamp(
                32f, 32f,
                1920f - 32f, 1080f - 32f
            );
            return pos.Equals(posScreen);
        }
    }

    public static class ComparableExtensions {
        /// <summary>
        /// Same as CompareTo but returns null instead of 0 if both items are equal.
        /// </summary>
        /// <typeparam name="T">IComparable type.</typeparam>
        /// <param name="this">This instance.</param>
        /// <param name="other">The other instance.</param>
        /// <returns>Lexical relation between this and the other instance or null if both are equal.</returns>
        /// 
        /// Source: https://stackoverflow.com/a/26890988
        public static int? NullableCompareTo<T>(this T obj, T other) where T : IComparable {
            var result = obj.CompareTo(other);
            return result != 0 ? result : null;
        }
    }
}
