using BarelyUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.UI
{
    class GeneratingCancelButton : Button
    {
        private bool isGenerating = false;

        public GeneratingCancelButton() : base("Cancel")
        {
            GeneratingStoped();
        }

        public void GeneratingStarted()
        {
            isGenerating = true;
            Interactable = true;
            ChangeColor(ButtonColors.Normal);
        }

        public void GeneratingStoped()
        {
            isGenerating = false;
            Interactable = false;
            ChangeColor(ButtonColors.Inactive);
        }

    }
}
