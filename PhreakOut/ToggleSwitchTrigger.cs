using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PhreakOut
{
    /// <summary>
    /// A <see cref="ToggleSwitchTrigger"/> triggers either in the ON or OFF state of a ToggleSwitch, 
    /// depending on if the <see cref="ToggleSwitchTrigger.TriggerOn"/> field is set to true or false.
    /// </summary>
    public class ToggleSwitchTrigger : StateTriggerBase
    {
        private ToggleSwitch _switch;

        /// <summary>
        /// ToggleSwitch that this trigger should trigger on
        /// </summary>
        public ToggleSwitch Switch { get { return _switch; } set {
                if (_switch != null)
                    _switch.Toggled -= TSwitch_Toggled;
                _switch = value;
                _switch.Toggled += TSwitch_Toggled;
                TSwitch_Toggled(_switch, null);
                System.Diagnostics.Debug.WriteLine("xxx");
            } }

        /// <summary>
        /// True if this should trigger when the <see cref="ToggleSwitch"/> this is bound to.
        /// </summary>
        public Boolean TriggerOn { get; set; }

        public ToggleSwitchTrigger() { /* noop */ }

        private void TSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("... changed event... ");
            SetActive(_switch.IsOn == TriggerOn);
        }
    }
}
