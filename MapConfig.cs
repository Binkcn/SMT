﻿
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;


namespace SMT
{

    public class MapConfig : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }


        private bool m_ShowNPCKills;
        private bool m_ShowPodKills;
        private bool m_ShowShipKills;
        private bool m_ShowShipJumps;

        [Browsable(false)]
        public string DefaultRegion;

        [Browsable(false)]
        public string DefaultColourSchemeName;


        [Browsable(false)]
        public List<MapColours> MapColours;

        [Browsable(false)]
        public MapColours ActiveColourScheme;



        [Category("Jump Bridges")]
        [DisplayName("Friendly Bridges")]
        public bool ShowJumpBridges { get; set; }

        [Category("Jump Bridges")]
        [DisplayName("Hostile Bridges")]
        public bool ShowHostileJumpBridges { get; set; }


        [Category("General")]
        [DisplayName("System Popup")]
        public bool ShowSystemPopup { get; set; }


        [Category("General")]
        [DisplayName("Show Security")]
        public bool ShowSystemSecurity { get; set; }

        [Category("ESI Data")]
        [DisplayName("ESI Scale")]
        public double ESIOverlayScale { get; set; }

        [Category("Intel")]
        [DisplayName("Show Intel")]
        public bool ShowIntel { get; set; }

        [Category("ESI Data")]
        [DisplayName("Rat Kills")]
        public bool ShowNPCKills
        {
            get
            {
                return m_ShowNPCKills;
            }
            set
            {
                m_ShowNPCKills = value;

                if (m_ShowNPCKills)
                {
                    ShowPodKills = false;
                    ShowShipKills = false;
                    ShowShipJumps = false;


                }

                OnPropertyChanged("ShowNPCKills");
            }
        }

        [Category("ESI Data")]
        [DisplayName("Pod Kills")]
        public bool ShowPodKills
        {
            get
            {
                return m_ShowPodKills;
            }
            set
            {
                m_ShowPodKills = value;
                if (m_ShowPodKills)
                {
                    ShowNPCKills = false;
                    ShowShipKills = false;
                    ShowShipJumps = false;

                }

                OnPropertyChanged("ShowPodKills");
            }
        }

        [Category("ESI Data")]
        [DisplayName("Ship Kills")]
        public bool ShowShipKills
        {
            get
            {
                return m_ShowShipKills;
            }
            set
            {
                m_ShowShipKills = value;
                if (m_ShowShipKills)
                {
                    ShowNPCKills = false;
                    ShowPodKills = false;
                    ShowShipJumps = false;
                }

                OnPropertyChanged("ShowShipKills");
            }
        }

        [Category("ESI Data")]
        [DisplayName("Jumps")]
        public bool ShowShipJumps
        {
            get
            {
                return m_ShowShipJumps;
            }
            set
            {
                m_ShowShipJumps = value;
                if (m_ShowShipJumps)
                {
                    ShowNPCKills = false;
                    ShowPodKills = false;
                    ShowShipKills = false;


                }

                OnPropertyChanged("ShowShipJumps");
            }
        }



        public void SetDefaults()
        {
            ShowJumpBridges = true;
            ShowHostileJumpBridges = true;
            ESIOverlayScale = 1.0f;
            ShowPodKills = true;
            ShowIntel = true;
            DefaultRegion = "Molden Heath";
            ShowSystemPopup = true;
            ShowSystemSecurity = true;

            MapColours = new List<MapColours>();
        }

        public void SetDefaultColours()
        {
            // 
            MapColours defaultColours = new MapColours();
            defaultColours.Name = "Default";
            defaultColours.UserEditable = false;
            defaultColours.FriendlyJumpBridgeColour = Color.FromRgb(102, 205, 170);
            defaultColours.HostileJumpBridgeColour = Color.FromRgb(250, 128, 114);
            defaultColours.SystemOutlineColour = Color.FromRgb(0, 0, 0);
            defaultColours.InRegionSystemColour = Color.FromRgb(255, 239, 213);
            defaultColours.InRegionSystemTextColour = Color.FromRgb(0, 0, 0);
            defaultColours.OutRegionSystemColour = Color.FromRgb(218, 165, 32);
            defaultColours.OutRegionSystemTextColour = Color.FromRgb(0, 0, 0);
            defaultColours.MapBackgroundColour = Color.FromRgb(105, 105, 105);
            defaultColours.ESIOverlayColour = Color.FromRgb(188, 143, 143);
            defaultColours.IntelOverlayColour = Color.FromRgb(178, 34, 34);
            defaultColours.NormalGateColour = Color.FromRgb(255, 248, 220);
            defaultColours.ConstellationGateColour = Color.FromRgb(128, 128, 128);
            defaultColours.SelectedSystemColour = Color.FromRgb(255, 255, 255);
            defaultColours.CharacterHighlightColour = Color.FromRgb(70, 130, 180);
            defaultColours.CharacterTextColour = Color.FromRgb(0, 0, 0);
            defaultColours.CharacterTextSize = 8;
            defaultColours.SystemTextSize = 12;
            MapColours.Add(defaultColours);

            MapColours blueColours = new MapColours();
            blueColours.Name = "Blue";
            blueColours.UserEditable = false;
            blueColours.FriendlyJumpBridgeColour = Color.FromRgb(154, 205, 50);
            blueColours.HostileJumpBridgeColour = Color.FromRgb(216, 191, 216);
            blueColours.SystemOutlineColour = Color.FromRgb(0, 0, 0);
            blueColours.InRegionSystemColour = Color.FromRgb(134, 206, 235);
            blueColours.InRegionSystemTextColour = Color.FromRgb(0, 0, 0);
            blueColours.OutRegionSystemColour = Color.FromRgb(112, 108, 124);
            blueColours.OutRegionSystemTextColour = Color.FromRgb(0, 0, 0);
            blueColours.MapBackgroundColour = Color.FromRgb(245, 245, 245);
            blueColours.ESIOverlayColour = Color.FromRgb(192, 192, 192);
            blueColours.IntelOverlayColour = Color.FromRgb(216, 191, 216);
            blueColours.NormalGateColour = Color.FromRgb(90, 90, 90);
            blueColours.ConstellationGateColour = Color.FromRgb(120, 120, 120);
            blueColours.SelectedSystemColour = Color.FromRgb(0, 0, 0);
            blueColours.CharacterHighlightColour = Color.FromRgb(0, 0, 0);
            blueColours.CharacterTextColour = Color.FromRgb(0, 0, 0);
            blueColours.CharacterTextSize = 8;
            blueColours.SystemTextSize = 12;
            MapColours.Add(blueColours);

            /*
            MapColours asimovColours = new MapColours();
            asimovColours.Name = "Asimov";
            asimovColours.UserEditable = false;
            asimovColours.FriendlyJumpBridgeColour = Color.FromRgb(105, 105, 105);
            asimovColours.HostileJumpBridgeColour = Color.FromRgb(216, 191, 216);
            asimovColours.SystemOutlineColour = Color.FromRgb(0, 0, 0);
            asimovColours.InRegionSystemColour = Color.FromRgb(255, 127, 80);
            asimovColours.InRegionSystemTextColour = Color.FromRgb(0, 0, 0);
            asimovColours.OutRegionSystemColour = Color.FromRgb(255, 165, 0);
            asimovColours.OutRegionSystemTextColour = Color.FromRgb(0, 0, 0);
            asimovColours.MapBackgroundColour = Color.FromRgb(245, 245, 245);
            asimovColours.ESIOverlayColour = Color.FromRgb(0, 0, 0);
            asimovColours.IntelOverlayColour = Color.FromRgb(255, 165, 0);
            asimovColours.NormalGateColour = Color.FromRgb(245, 222, 179);
            asimovColours.ConstellationGateColour = Color.FromRgb(245, 222, 179);
            asimovColours.SelectedSystemColour = Color.FromRgb(0, 0, 0);
            asimovColours.CharacterHighlightColour = Color.FromRgb(0, 0, 0);
            asimovColours.CharacterTextColour = Color.FromRgb(0, 0, 0);
            asimovColours.CharacterTextSize = 8;
            asimovColours.SystemTextSize = 12;
            MapColours.Add(asimovColours);
            */

            MapColours greyAndRed = new MapColours();
            greyAndRed.Name = "Grey & Red";
            greyAndRed.UserEditable = false;
            greyAndRed.FriendlyJumpBridgeColour = Color.FromRgb(128, 128, 128);
            greyAndRed.HostileJumpBridgeColour = Color.FromRgb(178, 34, 34);
            greyAndRed.SystemOutlineColour = Color.FromRgb(0, 0, 0);
            greyAndRed.InRegionSystemColour = Color.FromRgb(240, 248, 255);
            greyAndRed.InRegionSystemTextColour = Color.FromRgb(0, 0, 0);
            greyAndRed.OutRegionSystemColour = Color.FromRgb(128, 34, 34);
            greyAndRed.OutRegionSystemTextColour = Color.FromRgb(0, 0, 0);
            greyAndRed.MapBackgroundColour = Color.FromRgb(245, 245, 245);
            greyAndRed.ESIOverlayColour = Color.FromRgb(192, 192, 192);
            greyAndRed.IntelOverlayColour = Color.FromRgb(80, 34, 34);
            greyAndRed.NormalGateColour = Color.FromRgb(80, 80, 80);
            greyAndRed.ConstellationGateColour = Color.FromRgb(120, 120, 120);
            greyAndRed.SelectedSystemColour = Color.FromRgb(0, 0, 0);
            greyAndRed.CharacterHighlightColour = Color.FromRgb(0, 0, 0);
            greyAndRed.CharacterTextColour = Color.FromRgb(0, 0, 0);
            greyAndRed.CharacterTextSize = 8;
            greyAndRed.SystemTextSize = 12;
            MapColours.Add(greyAndRed);


            MapColours dark = new MapColours();
            dark.Name = "Dark";
            dark.UserEditable = false;
            dark.FriendlyJumpBridgeColour = Color.FromRgb(46, 139, 87);
            dark.HostileJumpBridgeColour = Color.FromRgb(178, 34, 34);
            dark.SystemOutlineColour = Color.FromRgb(0, 0, 0);
            dark.InRegionSystemColour = Color.FromRgb(112, 128, 144);
            dark.InRegionSystemTextColour = Color.FromRgb(128, 128, 128);
            dark.OutRegionSystemColour = Color.FromRgb(224, 255, 255);
            dark.OutRegionSystemTextColour = Color.FromRgb(128, 128, 128);
            dark.MapBackgroundColour = Color.FromRgb(20, 20, 20);
            dark.ESIOverlayColour = Color.FromRgb(209, 201, 202);
            dark.IntelOverlayColour = Color.FromRgb(205, 92, 92);
            dark.NormalGateColour = Color.FromRgb(192, 192, 192);
            dark.ConstellationGateColour = Color.FromRgb(150, 150, 150);
            dark.SelectedSystemColour = Color.FromRgb(255, 255, 255);
            dark.CharacterHighlightColour = Color.FromRgb(255, 255, 255);
            dark.CharacterTextColour = Color.FromRgb(255, 255, 255);
            dark.CharacterTextSize = 8;
            dark.SystemTextSize = 12;
            MapColours.Add(dark);



            ActiveColourScheme = blueColours;

        }

        public MapConfig()
        {
            SetDefaults();
        }
    }



}
