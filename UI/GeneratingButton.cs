using BarelyUI;
using Industry.Scenes;

namespace Industry.UI
{
    public class GeneratingButton : Button
    {
        private MapScene scene;
        private bool wasAlreadyGenerating = false;
        private float generatingTimer = 0.0f;
        private const float DOT_CHANGE_TIME = 0.33f;
        private const int DOT_STAGES = 4;
        private int generatingDots = 0;

        public GeneratingButton(string caption, MapScene scene) : base(caption)
        {
            this.scene = scene;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (scene.IsGeneratingMap())
            {                            
                generatingTimer += deltaTime;
                if (generatingTimer > DOT_CHANGE_TIME)
                {
                    generatingDots = (generatingDots + 1) % DOT_STAGES;
                    generatingTimer -= DOT_CHANGE_TIME;

                    if (generatingDots == 0)
                        ChangeText("Generating   ");
                    else if (generatingDots == 1)
                        ChangeText("Generating.  ");
                    else if (generatingDots == 2)
                        ChangeText("Generating.. ");
                    else if (generatingDots == 3)
                        ChangeText("Generating...");
                }

                wasAlreadyGenerating = true;
            } else
            {
                if (wasAlreadyGenerating)
                {
                    ChangeText("Generate");
                    generatingTimer = 0.0f;
                    generatingDots = 0;
                }
                wasAlreadyGenerating = false;
            }

        }



    }
}
