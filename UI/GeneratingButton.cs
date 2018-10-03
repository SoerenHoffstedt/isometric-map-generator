using BarelyUI;
using Industry.Scenes;

namespace Industry.UI
{
    public class GeneratingButton : Button
    {
        private const float DOT_CHANGE_TIME = 0.33f;
        private const int DOT_STAGES = 4;
        private readonly string[] DOT_TEXTS = { "Generating   ", "Generating.  ", "Generating.. ", "Generating..." };
        private string STD_TEXT = "GENERATE";
        private int generatingDots = 0;
        private float generatingTimer = 0.0f;
        private bool isGenerating = false;
        
        public GeneratingButton() : base("Generate")
        {
            
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (isGenerating)
            {                            
                generatingTimer += deltaTime;
                if (generatingTimer > DOT_CHANGE_TIME)
                {
                    generatingDots = (generatingDots + 1) % DOT_STAGES;
                    generatingTimer -= DOT_CHANGE_TIME;
                    ChangeText(DOT_TEXTS[generatingDots]);
                }                
            }
        }

        public void GeneratingStarted()
        {
            isGenerating = true;
            ChangeColor(ButtonColors.Inactive);
            Interactable = false;
            ChangeText(DOT_TEXTS[0]);
        }

        public void GeneratingStoped()
        {
            Interactable = true;
            ChangeColor(ButtonColors.Normal);
            isGenerating = false;
            ChangeText(STD_TEXT);
            generatingTimer = 0.0f;
            generatingDots = 0;
        }



    }
}
