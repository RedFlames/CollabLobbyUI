using System;
using Microsoft.Xna.Framework;
using Monocle;
using Color = Microsoft.Xna.Framework.Color;

namespace Celeste.Mod.CollabLobbyUI {
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

            return GetClampedScreenPos(e.Center, l, out pos);
        }

        public static bool GetClampedScreenPos(Vector2 p, Level l, out Vector2 pos) {
            if (l == null) {
                pos = Vector2.Zero;
                return false;
            }
            Vector2 posScreen = l.WorldToScreen(p);
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
            var result = obj?.CompareTo(other);
            return result != 0 ? result : null;
        }
    }

    public static class MTextureExtensions {

        public static void DrawOnCenterLine( this MTexture tex, Vector2 position, float scale = 1f, Color? color = null, float justifyX = 0f) {
            tex.Draw(position, new(justifyX * tex.Width, tex.Height / 2f), color ?? Color.White, scale);
        }

        public static void DrawOnCenterLineScaled(this MTexture tex, Vector2 position, float targetWidth, Color? color = null, float justifyX = 0f) {
            tex.Draw(position, new(justifyX * tex.Width, tex.Height / 2f), color ?? Color.White, targetWidth / tex.Width);
        }
    }
}
