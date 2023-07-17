namespace Celeste.Mod.CollabLobbyUI {
    public class InputRepeatDelay {
        /* A class to implement a button input delay that when held
         * - allows the action to perform right away
         * - then has an inital delay before triggering again
         * - after that uses a faster moving delay to trigger
         * e.g. like when holding down Left/Right Arrow Key in a text editor.
         */

        public ButtonBinding InputA { get; private set; }
        public ButtonBinding InputB { get; private set; }

        public float InitialDelay { get; private set; }
        public float MoveDelay { get; private set; }

        protected bool IsDownA => InputA.Check;
        protected bool IsDownB => InputB.Check;

        public bool CanMove => timeSinceMoved > (moveFast ? MoveDelay : InitialDelay) || !useInitialDelay;

        private bool useInitialDelay = false, moveFast = false;
        private float timeSinceMoved = 0;

        public InputRepeatDelay(ButtonBinding A, ButtonBinding B = null, float initialDelay = 0.3f, float moveDelay = 0.05f) {
            SetBinds(A, B);
            SetDelays(initialDelay, moveDelay);
        }

        public void SetBinds(ButtonBinding A, ButtonBinding B = null) {
            // can be used with a single button, where InputB gets set to A also
            InputA = A;
            InputB = (B != null) ? B : A;
        }

        public void SetDelays(float initialDelay, float moveDelay) {
            InitialDelay = initialDelay;
            MoveDelay = moveDelay;
        }

        public void Update(float time) {
            timeSinceMoved += time;

            if (!IsDownA && !IsDownB) {
                // always reset so that first press has no delays
                timeSinceMoved = 0;
                useInitialDelay = false;
                moveFast = false;
            }
        }

        public bool Check(ButtonBinding bb) {
            if (bb != InputA && bb != InputB)
                return false;
            return bb.Check && CanMove;
        }

        public void Triggered() {
            timeSinceMoved = 0;
            // this is where moveFast gets set, so that MoveDelay only gets used after first trigger had InitialDelay
            if (!useInitialDelay)
                useInitialDelay = true;
            else
                moveFast = true;
        }
    }
}
