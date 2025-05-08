using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace VMS.TPS
{
    public class Script
    {
        private string errMessage = "";
        public void Execute(ScriptContext scriptContext)
        {
            Run(scriptContext.Patient, scriptContext.PlanSetup, scriptContext.StructureSet);
        }
        private void Run(Patient _patient, PlanSetup _plan, StructureSet _structureSet)
        {
            //Plankontroller
            CheckPlanApprovalStatus(_plan);

            //Fältkontroller
            CheckFieldDoseRate(_plan);
            CheckFieldNames(_plan);
            CheckIsoCenterGroup(_plan);

            //Strukturkontroller

            if (errMessage == string.Empty)
            {
                MessageBox.Show("Alla checkar OK!");
            }
            else
            {
                MessageBox.Show(errMessage);
            }
        }
        private void CheckPlanApprovalStatus(PlanSetup _plan)
        {
            bool ok = true;

            if (_plan.ApprovalStatus != PlanSetupApprovalStatus.PlanningApproved)
            {
                ok = false;
            }

            if (ok == false)
            {
                errMessage += "Planen är inte planning approved\n";
            }
        }
        private void CheckFieldDoseRate(PlanSetup _plan)
        {

            bool ok = true;

            foreach (Beam beam in _plan.Beams.Where(b => !b.IsSetupField))
            {
                if ((beam.EnergyModeDisplayName.Equals("6X") && !beam.DoseRate.Equals(600)) ||
                    (beam.EnergyModeDisplayName.Equals("15X") && !beam.DoseRate.Equals(600)) ||
                    (beam.EnergyModeDisplayName.Equals("10X-FFF") && !beam.DoseRate.Equals(2400)) ||
                    (beam.EnergyModeDisplayName.Equals("6X-FFF") && !beam.DoseRate.Equals(1400)))
                {
                    ok = false;
                }
            }

            if (ok == false)
            {
                errMessage += "Dosraten är inte standard för något fält\n";
            }
        }
        private void CheckFieldNames(PlanSetup _plan)
        {
            bool ok = true; //Om något är fel kommer ok bytas till false

            //Kör testet
            foreach (Beam beam in _plan.Beams)
            {
                if (!beam.Id.Contains("CBCT"))//Kontroller gäller ej för CBCT-fältet
                {
                    string gantryAngle = beam.ControlPoints.First().GantryAngle.ToString();
                    if (beam.MLCPlanType.ToString() == "VMAT")
                    {
                        var gantryDirection = beam.GantryDirection;
                        if (gantryDirection == GantryDirection.Clockwise && !beam.Id.Contains("CW") || beam.Id.Contains("CCW")) //är den medurs men heter inte CW
                        {
                            ok = false;
                        }
                        else if (beam.GantryDirection == GantryDirection.CounterClockwise && !beam.Id.Contains("CCW")) //är den moturs men heter inte CCW
                        {
                            ok = false;
                        }
                    }
                    else if (!beam.Id.Contains(gantryAngle)) //Konventionella/IMRT fält, har den inte vinkeln i namnet
                    {
                        string[] words = beam.Id.Split(' ');//delar upp alla ord i fältnamnet för att kunna särskilja en 0 i 180 och 0 grader
                        bool match = false;
                        foreach (string word in words)
                        {
                            if (word.Equals(beam.ControlPoints.First().GantryAngle.ToString())) //om något ord matchar vinkeln är det okej
                                match = true;
                        }
                        if (match == false)
                        {
                            ok = false;
                        }
                    }
                }
            }

            if (ok == false)
            {
                errMessage += "Fältnamnen stämmer inte\n";
            }
        }
        public void CheckIsoCenterGroup(PlanSetup _plan)
        {
            bool ok = true;

            var iso = _plan.Beams.First().IsocenterPosition;
            foreach (Beam beam in _plan.Beams)
            {
                if (iso.x != beam.IsocenterPosition.x || iso.y != beam.IsocenterPosition.y || iso.z != beam.IsocenterPosition.z) //Om något fält inte är samma som första fältet
                {
                    ok = false;
                }
            }
            if (ok == false)
            {
                errMessage += "Alla fälts iso stämmer inte\n";
            }
        }
    }
}
