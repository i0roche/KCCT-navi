namespace team1
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Button1 = new Button();
            FlowLayoutPanel1 = new FlowLayoutPanel();
            MenuStrip1 = new MenuStrip();
            MenuItem_General = new ToolStripMenuItem();
            MenuItem_General_1F = new ToolStripMenuItem();
            MenuItem_General_2F = new ToolStripMenuItem();
            MenuItem_General_3F = new ToolStripMenuItem();
            MenuItem_General_4F = new ToolStripMenuItem();
            MenuItem_General_5F = new ToolStripMenuItem();
            MenuItem_General_6F = new ToolStripMenuItem();
            MenuItem_Senmon = new ToolStripMenuItem();
            MenuItem_Mech = new ToolStripMenuItem();
            MenuItem_Mech_1F = new ToolStripMenuItem();
            MenuItem_Mech_2F = new ToolStripMenuItem();
            MenuItem_Mech_3F = new ToolStripMenuItem();
            MenuItem_Mech_4F = new ToolStripMenuItem();
            MenuItem_Elec = new ToolStripMenuItem();
            MenuItem_Elec_1F = new ToolStripMenuItem();
            MenuItem_Elec_2F = new ToolStripMenuItem();
            MenuItem_Elec_3F = new ToolStripMenuItem();
            MenuItem_Elec_4F = new ToolStripMenuItem();
            MenuItem_Denshi = new ToolStripMenuItem();
            MenuItem_Denshi_1F = new ToolStripMenuItem();
            MenuItem_Denshi_2F = new ToolStripMenuItem();
            MenuItem_Denshi_3F = new ToolStripMenuItem();
            MenuItem_Denshi_4F = new ToolStripMenuItem();
            MenuItem_Toshi = new ToolStripMenuItem();
            MenuItem_Toshi_1F = new ToolStripMenuItem();
            MenuItem_Toshi_2F = new ToolStripMenuItem();
            MenuItem_Toshi_3F = new ToolStripMenuItem();
            MenuItem_Toshi_4F = new ToolStripMenuItem();
            MenuItem_Chem = new ToolStripMenuItem();
            MenuItem_Chem_F1 = new ToolStripMenuItem();
            MenuItem_Chem_2F = new ToolStripMenuItem();
            MenuItem_Chem_3F = new ToolStripMenuItem();
            MenuItem_Advanced = new ToolStripMenuItem();
            MenuItem_Advanced_1F = new ToolStripMenuItem();
            MenuItem_Advanced_2F = new ToolStripMenuItem();
            MenuItem_Advanced_3F = new ToolStripMenuItem();
            MenuItem_Honbu = new ToolStripMenuItem();
            MenuItem_Honbu_1F = new ToolStripMenuItem();
            MenuItem_Honbu_2F = new ToolStripMenuItem();
            MenuItem_Kougi = new ToolStripMenuItem();
            MenuItem_Kougi_1F = new ToolStripMenuItem();
            MenuItem_Kougi_2F = new ToolStripMenuItem();
            MenuItem_Kougi_3F = new ToolStripMenuItem();
            MenuItem_Kougi_4F = new ToolStripMenuItem();
            MenuItem_Gakusei = new ToolStripMenuItem();
            MenuItem_Gakusei_1F = new ToolStripMenuItem();
            MenuItem_Gakusei_2F = new ToolStripMenuItem();
            MenuItem_Gakusei_3F = new ToolStripMenuItem();
            MenuItem_Library = new ToolStripMenuItem();
            MenuItem_Library_1F = new ToolStripMenuItem();
            MenuItem_Library_2F = new ToolStripMenuItem();
            MenuItem_Chem_4F = new ToolStripMenuItem();
            textBox1 = new TextBox();
            panel1 = new Panel();
            panelBuilding = new Panel();
            buttonUnfavorite = new Button();
            buttonFavorite = new Button();
            labelFavoriteStatus = new Label();
            labelBuildingDescription = new Label();
            labelBuildingTitle = new Label();
            panelSearch = new Panel();
            listBoxFavorites = new ListBox();
            buttonFavoriteToGoal = new Button();
            buttonFavoriteToStart = new Button();
            listBoxGoal = new ListBox();
            listBoxStart = new ListBox();
            textBox2 = new TextBox();
            panelTabs = new Panel();
            buttonTabInfo = new Button();
            buttonTabSearch = new Button();
            MenuStrip1.SuspendLayout();
            panel1.SuspendLayout();
            panelBuilding.SuspendLayout();
            panelSearch.SuspendLayout();
            panelTabs.SuspendLayout();
            SuspendLayout();
            // 
            // Button1
            // 
            Button1.Location = new Point(90, 370);
            Button1.Margin = new Padding(2, 1, 2, 1);
            Button1.Name = "Button1";
            Button1.Size = new Size(81, 22);
            Button1.TabIndex = 1;
            Button1.Text = "検索";
            Button1.UseVisualStyleBackColor = false;
            Button1.Click += OnSearchButtonClick;
            // 
            // FlowLayoutPanel1
            // 
            FlowLayoutPanel1.AutoSize = true;
            FlowLayoutPanel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            FlowLayoutPanel1.Location = new Point(0, 0);
            FlowLayoutPanel1.Margin = new Padding(2, 1, 2, 1);
            FlowLayoutPanel1.Name = "FlowLayoutPanel1";
            FlowLayoutPanel1.Size = new Size(0, 0);
            FlowLayoutPanel1.TabIndex = 3;
            // 
            // MenuStrip1
            // 
            MenuStrip1.Font = new Font("メイリオ", 9F, FontStyle.Regular, GraphicsUnit.Point, 128);
            MenuStrip1.ImageScalingSize = new Size(20, 20);
            MenuStrip1.Items.AddRange(new ToolStripItem[] { MenuItem_General, MenuItem_Senmon, MenuItem_Advanced, MenuItem_Honbu, MenuItem_Kougi, MenuItem_Gakusei, MenuItem_Library });
            MenuStrip1.Location = new Point(0, 0);
            MenuStrip1.Name = "MenuStrip1";
            MenuStrip1.Padding = new Padding(5, 1, 0, 1);
            MenuStrip1.Size = new Size(951, 24);
            MenuStrip1.TabIndex = 5;
            MenuStrip1.Text = "menuStrip1";
            // 
            // MenuItem_General
            // 
            MenuItem_General.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_General_1F, MenuItem_General_2F, MenuItem_General_3F, MenuItem_General_4F, MenuItem_General_5F, MenuItem_General_6F });
            MenuItem_General.Name = "MenuItem_General";
            MenuItem_General.Size = new Size(68, 22);
            MenuItem_General.Text = "一般科棟";
            MenuItem_General.Click += OnGeneralBuildingMenuClick;
            // 
            // MenuItem_General_1F
            // 
            MenuItem_General_1F.Name = "MenuItem_General_1F";
            MenuItem_General_1F.Size = new Size(95, 22);
            MenuItem_General_1F.Text = "１F";
            MenuItem_General_1F.Click += OnGeneralBuildingFloor1Click;
            // 
            // MenuItem_General_2F
            // 
            MenuItem_General_2F.Name = "MenuItem_General_2F";
            MenuItem_General_2F.Size = new Size(95, 22);
            MenuItem_General_2F.Text = "２F";
            MenuItem_General_2F.Click += OnGeneralBuildingFloor2Click;
            // 
            // MenuItem_General_3F
            // 
            MenuItem_General_3F.Name = "MenuItem_General_3F";
            MenuItem_General_3F.Size = new Size(95, 22);
            MenuItem_General_3F.Text = "３F";
            MenuItem_General_3F.Click += OnGeneralBuildingFloor3Click;
            // 
            // MenuItem_General_4F
            // 
            MenuItem_General_4F.Name = "MenuItem_General_4F";
            MenuItem_General_4F.Size = new Size(95, 22);
            MenuItem_General_4F.Text = "４F";
            MenuItem_General_4F.Click += OnGeneralBuildingFloor4Click;
            // 
            // MenuItem_General_5F
            // 
            MenuItem_General_5F.Name = "MenuItem_General_5F";
            MenuItem_General_5F.Size = new Size(95, 22);
            MenuItem_General_5F.Text = "５F";
            MenuItem_General_5F.Click += OnGeneralBuildingFloor5Click;
            // 
            // MenuItem_General_6F
            // 
            MenuItem_General_6F.Name = "MenuItem_General_6F";
            MenuItem_General_6F.Size = new Size(95, 22);
            MenuItem_General_6F.Text = "６F";
            MenuItem_General_6F.Click += OnGeneralBuildingFloor6Click;
            // 
            // MenuItem_Senmon
            // 
            MenuItem_Senmon.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_Mech, MenuItem_Elec, MenuItem_Denshi, MenuItem_Toshi, MenuItem_Chem });
            MenuItem_Senmon.Name = "MenuItem_Senmon";
            MenuItem_Senmon.Size = new Size(80, 22);
            MenuItem_Senmon.Text = "専門学科棟";
            MenuItem_Senmon.Click += OnSpecializedBuildingMenuClick;
            // 
            // MenuItem_Mech
            // 
            MenuItem_Mech.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_Mech_1F, MenuItem_Mech_2F, MenuItem_Mech_3F, MenuItem_Mech_4F });
            MenuItem_Mech.Name = "MenuItem_Mech";
            MenuItem_Mech.Size = new Size(148, 22);
            MenuItem_Mech.Text = "機械工学科棟";
            // 
            // MenuItem_Mech_1F
            // 
            MenuItem_Mech_1F.Name = "MenuItem_Mech_1F";
            MenuItem_Mech_1F.Size = new Size(95, 22);
            MenuItem_Mech_1F.Text = "１F";
            MenuItem_Mech_1F.Click += OnMechBuildingFloor1Click;
            // 
            // MenuItem_Mech_2F
            // 
            MenuItem_Mech_2F.Name = "MenuItem_Mech_2F";
            MenuItem_Mech_2F.Size = new Size(95, 22);
            MenuItem_Mech_2F.Text = "２F";
            MenuItem_Mech_2F.Click += OnMechBuildingFloor2Click;
            // 
            // MenuItem_Mech_3F
            // 
            MenuItem_Mech_3F.Name = "MenuItem_Mech_3F";
            MenuItem_Mech_3F.Size = new Size(95, 22);
            MenuItem_Mech_3F.Text = "３F";
            MenuItem_Mech_3F.Click += OnMechBuildingFloor3Click;
            // 
            // MenuItem_Mech_4F
            // 
            MenuItem_Mech_4F.Name = "MenuItem_Mech_4F";
            MenuItem_Mech_4F.Size = new Size(95, 22);
            MenuItem_Mech_4F.Text = "４F";
            MenuItem_Mech_4F.Click += OnMechBuildingFloor4Click;
            // 
            // MenuItem_Elec
            // 
            MenuItem_Elec.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_Elec_1F, MenuItem_Elec_2F, MenuItem_Elec_3F, MenuItem_Elec_4F });
            MenuItem_Elec.Name = "MenuItem_Elec";
            MenuItem_Elec.Size = new Size(148, 22);
            MenuItem_Elec.Text = "電気工学科棟";
            MenuItem_Elec.Click += OnElecBuildingMenuClick;
            // 
            // MenuItem_Elec_1F
            // 
            MenuItem_Elec_1F.Name = "MenuItem_Elec_1F";
            MenuItem_Elec_1F.Size = new Size(95, 22);
            MenuItem_Elec_1F.Text = "１F";
            MenuItem_Elec_1F.Click += OnElecBuildingFloor1Click;
            // 
            // MenuItem_Elec_2F
            // 
            MenuItem_Elec_2F.Name = "MenuItem_Elec_2F";
            MenuItem_Elec_2F.Size = new Size(95, 22);
            MenuItem_Elec_2F.Text = "２F";
            MenuItem_Elec_2F.Click += OnElecBuildingFloor2Click;
            // 
            // MenuItem_Elec_3F
            // 
            MenuItem_Elec_3F.Name = "MenuItem_Elec_3F";
            MenuItem_Elec_3F.Size = new Size(95, 22);
            MenuItem_Elec_3F.Text = "３F";
            MenuItem_Elec_3F.Click += OnElecBuildingFloor3Click;
            // 
            // MenuItem_Elec_4F
            // 
            MenuItem_Elec_4F.Name = "MenuItem_Elec_4F";
            MenuItem_Elec_4F.Size = new Size(95, 22);
            MenuItem_Elec_4F.Text = "４F";
            MenuItem_Elec_4F.Click += OnElecBuildingFloor4Click;
            // 
            // MenuItem_Denshi
            // 
            MenuItem_Denshi.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_Denshi_1F, MenuItem_Denshi_2F, MenuItem_Denshi_3F, MenuItem_Denshi_4F });
            MenuItem_Denshi.Name = "MenuItem_Denshi";
            MenuItem_Denshi.Size = new Size(148, 22);
            MenuItem_Denshi.Text = "電子工学科棟";
            // 
            // MenuItem_Denshi_1F
            // 
            MenuItem_Denshi_1F.Name = "MenuItem_Denshi_1F";
            MenuItem_Denshi_1F.Size = new Size(95, 22);
            MenuItem_Denshi_1F.Text = "１F";
            MenuItem_Denshi_1F.Click += OnDenshiBuildingFloor1Click;
            // 
            // MenuItem_Denshi_2F
            // 
            MenuItem_Denshi_2F.Name = "MenuItem_Denshi_2F";
            MenuItem_Denshi_2F.Size = new Size(95, 22);
            MenuItem_Denshi_2F.Text = "２F";
            MenuItem_Denshi_2F.Click += OnDenshiBuildingFloor2Click;
            // 
            // MenuItem_Denshi_3F
            // 
            MenuItem_Denshi_3F.Name = "MenuItem_Denshi_3F";
            MenuItem_Denshi_3F.Size = new Size(95, 22);
            MenuItem_Denshi_3F.Text = "３F";
            MenuItem_Denshi_3F.Click += OnDenshiBuildingFloor3Click;
            // 
            // MenuItem_Denshi_4F
            // 
            MenuItem_Denshi_4F.Name = "MenuItem_Denshi_4F";
            MenuItem_Denshi_4F.Size = new Size(95, 22);
            MenuItem_Denshi_4F.Text = "４F";
            MenuItem_Denshi_4F.Click += OnDenshiBuildingFloor4Click;
            // 
            // MenuItem_Toshi
            // 
            MenuItem_Toshi.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_Toshi_1F, MenuItem_Toshi_2F, MenuItem_Toshi_3F, MenuItem_Toshi_4F });
            MenuItem_Toshi.Name = "MenuItem_Toshi";
            MenuItem_Toshi.Size = new Size(148, 22);
            MenuItem_Toshi.Text = "都市工学科棟";
            // 
            // MenuItem_Toshi_1F
            // 
            MenuItem_Toshi_1F.Name = "MenuItem_Toshi_1F";
            MenuItem_Toshi_1F.Size = new Size(95, 22);
            MenuItem_Toshi_1F.Text = "１F";
            MenuItem_Toshi_1F.Click += OnToshiBuildingFloor1Click;
            // 
            // MenuItem_Toshi_2F
            // 
            MenuItem_Toshi_2F.Name = "MenuItem_Toshi_2F";
            MenuItem_Toshi_2F.Size = new Size(95, 22);
            MenuItem_Toshi_2F.Text = "２F";
            MenuItem_Toshi_2F.Click += OnToshiBuildingFloor2Click;
            // 
            // MenuItem_Toshi_3F
            // 
            MenuItem_Toshi_3F.Name = "MenuItem_Toshi_3F";
            MenuItem_Toshi_3F.Size = new Size(95, 22);
            MenuItem_Toshi_3F.Text = "３F";
            MenuItem_Toshi_3F.Click += OnToshiBuildingFloor3Click;
            // 
            // MenuItem_Toshi_4F
            // 
            MenuItem_Toshi_4F.Name = "MenuItem_Toshi_4F";
            MenuItem_Toshi_4F.Size = new Size(95, 22);
            MenuItem_Toshi_4F.Text = "３F";
            MenuItem_Toshi_4F.Click += OnToshiBuildingFloor4Click;
            // 
            // MenuItem_Chem
            // 
            MenuItem_Chem.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_Chem_F1, MenuItem_Chem_2F, MenuItem_Chem_3F });
            MenuItem_Chem.Name = "MenuItem_Chem";
            MenuItem_Chem.Size = new Size(148, 22);
            MenuItem_Chem.Text = "応用化学科棟";
            MenuItem_Chem.Click += OnChemBuildingMenuClick;
            // 
            // MenuItem_Chem_F1
            // 
            MenuItem_Chem_F1.Name = "MenuItem_Chem_F1";
            MenuItem_Chem_F1.Size = new Size(95, 22);
            MenuItem_Chem_F1.Text = "１F";
            MenuItem_Chem_F1.Click += OnChemBuildingFloor1Click;
            // 
            // MenuItem_Chem_2F
            // 
            MenuItem_Chem_2F.Name = "MenuItem_Chem_2F";
            MenuItem_Chem_2F.Size = new Size(95, 22);
            MenuItem_Chem_2F.Text = "２F";
            MenuItem_Chem_2F.Click += OnChemBuildingFloor2Click;
            // 
            // MenuItem_Chem_3F
            // 
            MenuItem_Chem_3F.Name = "MenuItem_Chem_3F";
            MenuItem_Chem_3F.Size = new Size(95, 22);
            MenuItem_Chem_3F.Text = "３F";
            MenuItem_Chem_3F.Click += OnChemBuildingFloor3Click;
            // 
            // MenuItem_Advanced
            // 
            MenuItem_Advanced.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_Advanced_1F, MenuItem_Advanced_2F, MenuItem_Advanced_3F });
            MenuItem_Advanced.Name = "MenuItem_Advanced";
            MenuItem_Advanced.Size = new Size(68, 22);
            MenuItem_Advanced.Text = "専攻科棟";
            MenuItem_Advanced.Click += OnAdvancedBuildingMenuClick;
            // 
            // MenuItem_Advanced_1F
            // 
            MenuItem_Advanced_1F.Name = "MenuItem_Advanced_1F";
            MenuItem_Advanced_1F.Size = new Size(95, 22);
            MenuItem_Advanced_1F.Text = "１F";
            MenuItem_Advanced_1F.Click += OnAdvancedBuildingFloor1Click;
            // 
            // MenuItem_Advanced_2F
            // 
            MenuItem_Advanced_2F.Name = "MenuItem_Advanced_2F";
            MenuItem_Advanced_2F.Size = new Size(95, 22);
            MenuItem_Advanced_2F.Text = "２F";
            MenuItem_Advanced_2F.Click += OnAdvancedBuildingFloor2Click;
            // 
            // MenuItem_Advanced_3F
            // 
            MenuItem_Advanced_3F.Name = "MenuItem_Advanced_3F";
            MenuItem_Advanced_3F.Size = new Size(95, 22);
            MenuItem_Advanced_3F.Text = "３F";
            MenuItem_Advanced_3F.Click += OnAdvancedBuildingFloor3Click;
            // 
            // MenuItem_Honbu
            // 
            MenuItem_Honbu.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_Honbu_1F, MenuItem_Honbu_2F });
            MenuItem_Honbu.Name = "MenuItem_Honbu";
            MenuItem_Honbu.Size = new Size(56, 22);
            MenuItem_Honbu.Text = "本部棟";
            MenuItem_Honbu.Click += OnHonbuBuildingMenuClick;
            // 
            // MenuItem_Honbu_1F
            // 
            MenuItem_Honbu_1F.Name = "MenuItem_Honbu_1F";
            MenuItem_Honbu_1F.Size = new Size(95, 22);
            MenuItem_Honbu_1F.Text = "１F";
            MenuItem_Honbu_1F.Click += OnHonbuBuildingFloor1Click;
            // 
            // MenuItem_Honbu_2F
            // 
            MenuItem_Honbu_2F.Name = "MenuItem_Honbu_2F";
            MenuItem_Honbu_2F.Size = new Size(95, 22);
            MenuItem_Honbu_2F.Text = "２F";
            MenuItem_Honbu_2F.Click += OnHonbuBuildingFloor2Click;
            // 
            // MenuItem_Kougi
            // 
            MenuItem_Kougi.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_Kougi_1F, MenuItem_Kougi_2F, MenuItem_Kougi_3F, MenuItem_Kougi_4F });
            MenuItem_Kougi.Name = "MenuItem_Kougi";
            MenuItem_Kougi.Size = new Size(56, 22);
            MenuItem_Kougi.Text = "講義棟";
            MenuItem_Kougi.Click += OnKougiBuildingMenuClick;
            // 
            // MenuItem_Kougi_1F
            // 
            MenuItem_Kougi_1F.Name = "MenuItem_Kougi_1F";
            MenuItem_Kougi_1F.Size = new Size(95, 22);
            MenuItem_Kougi_1F.Text = "１F";
            MenuItem_Kougi_1F.Click += OnKougiBuildingFloor1Click;
            // 
            // MenuItem_Kougi_2F
            // 
            MenuItem_Kougi_2F.Name = "MenuItem_Kougi_2F";
            MenuItem_Kougi_2F.Size = new Size(95, 22);
            MenuItem_Kougi_2F.Text = "２F";
            MenuItem_Kougi_2F.Click += OnKougiBuildingFloor2Click;
            // 
            // MenuItem_Kougi_3F
            // 
            MenuItem_Kougi_3F.Name = "MenuItem_Kougi_3F";
            MenuItem_Kougi_3F.Size = new Size(95, 22);
            MenuItem_Kougi_3F.Text = "３F";
            MenuItem_Kougi_3F.Click += OnKougiBuildingFloor3Click;
            // 
            // MenuItem_Kougi_4F
            // 
            MenuItem_Kougi_4F.Name = "MenuItem_Kougi_4F";
            MenuItem_Kougi_4F.Size = new Size(95, 22);
            MenuItem_Kougi_4F.Text = "４F";
            MenuItem_Kougi_4F.Click += OnKougiBuildingFloor4Click;
            // 
            // MenuItem_Gakusei
            // 
            MenuItem_Gakusei.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_Gakusei_1F, MenuItem_Gakusei_2F, MenuItem_Gakusei_3F });
            MenuItem_Gakusei.Name = "MenuItem_Gakusei";
            MenuItem_Gakusei.Size = new Size(68, 22);
            MenuItem_Gakusei.Text = "学生会館";
            MenuItem_Gakusei.Click += OnGakuseiBuildingMenuClick;
            // 
            // MenuItem_Gakusei_1F
            // 
            MenuItem_Gakusei_1F.Name = "MenuItem_Gakusei_1F";
            MenuItem_Gakusei_1F.Size = new Size(95, 22);
            MenuItem_Gakusei_1F.Text = "１F";
            MenuItem_Gakusei_1F.Click += OnGakuseiBuildingFloor1Click;
            // 
            // MenuItem_Gakusei_2F
            // 
            MenuItem_Gakusei_2F.Name = "MenuItem_Gakusei_2F";
            MenuItem_Gakusei_2F.Size = new Size(95, 22);
            MenuItem_Gakusei_2F.Text = "２F";
            MenuItem_Gakusei_2F.Click += OnGakuseiBuildingFloor2Click;
            // 
            // MenuItem_Gakusei_3F
            // 
            MenuItem_Gakusei_3F.Name = "MenuItem_Gakusei_3F";
            MenuItem_Gakusei_3F.Size = new Size(95, 22);
            MenuItem_Gakusei_3F.Text = "３F";
            MenuItem_Gakusei_3F.Click += OnGakuseiBuildingFloor3Click;
            // 
            // MenuItem_Library
            // 
            MenuItem_Library.DropDownItems.AddRange(new ToolStripItem[] { MenuItem_Library_1F, MenuItem_Library_2F });
            MenuItem_Library.Name = "MenuItem_Library";
            MenuItem_Library.Size = new Size(56, 22);
            MenuItem_Library.Text = "図書館";
            MenuItem_Library.Click += OnLibraryBuildingMenuClick;
            // 
            // MenuItem_Library_1F
            // 
            MenuItem_Library_1F.Name = "MenuItem_Library_1F";
            MenuItem_Library_1F.Size = new Size(95, 22);
            MenuItem_Library_1F.Text = "１F";
            MenuItem_Library_1F.Click += OnLibraryBuildingFloor1Click;
            // 
            // MenuItem_Library_2F
            // 
            MenuItem_Library_2F.Name = "MenuItem_Library_2F";
            MenuItem_Library_2F.Size = new Size(95, 22);
            MenuItem_Library_2F.Text = "２F";
            MenuItem_Library_2F.Click += OnLibraryBuildingFloor2Click;
            // 
            // MenuItem_Chem_4F
            // 
            MenuItem_Chem_4F.Name = "MenuItem_Chem_4F";
            MenuItem_Chem_4F.Size = new Size(183, 44);
            MenuItem_Chem_4F.Text = "４F";
            MenuItem_Chem_4F.Click += OnChemBuildingFloor4Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(12, 16);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(159, 23);
            textBox1.TabIndex = 6;
            textBox1.TextChanged += OnStartTextChanged;
            textBox1.Enter += OnSearchPanelRequested;
            textBox1.KeyDown += OnSearchTextBoxKeyDown;
            // 
            // panel1
            // 
            panel1.Controls.Add(panelBuilding);
            panel1.Controls.Add(panelSearch);
            panel1.Controls.Add(panelTabs);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 24);
            panel1.Name = "panel1";
            panel1.Size = new Size(182, 592);
            panel1.TabIndex = 7;
            panel1.Visible = false;
            // 
            // panelBuilding
            // 
            panelBuilding.Controls.Add(buttonUnfavorite);
            panelBuilding.Controls.Add(buttonFavorite);
            panelBuilding.Controls.Add(labelFavoriteStatus);
            panelBuilding.Controls.Add(labelBuildingDescription);
            panelBuilding.Controls.Add(labelBuildingTitle);
            panelBuilding.Dock = DockStyle.Fill;
            panelBuilding.Location = new Point(0, 32);
            panelBuilding.Name = "panelBuilding";
            panelBuilding.Size = new Size(182, 560);
            panelBuilding.TabIndex = 9;
            panelBuilding.Visible = false;
            // 
            // buttonUnfavorite
            // 
            buttonUnfavorite.Location = new Point(96, 132);
            buttonUnfavorite.Name = "buttonUnfavorite";
            buttonUnfavorite.Size = new Size(75, 23);
            buttonUnfavorite.TabIndex = 4;
            buttonUnfavorite.Text = "解除";
            buttonUnfavorite.UseVisualStyleBackColor = true;
            buttonUnfavorite.Click += OnFavoriteRemoveClick;
            // 
            // buttonFavorite
            // 
            buttonFavorite.Location = new Point(12, 132);
            buttonFavorite.Name = "buttonFavorite";
            buttonFavorite.Size = new Size(75, 23);
            buttonFavorite.TabIndex = 3;
            buttonFavorite.Text = "お気に入り";
            buttonFavorite.UseVisualStyleBackColor = true;
            buttonFavorite.Click += OnFavoriteAddClick;
            // 
            // labelFavoriteStatus
            // 
            labelFavoriteStatus.AutoSize = true;
            labelFavoriteStatus.Location = new Point(12, 108);
            labelFavoriteStatus.Name = "labelFavoriteStatus";
            labelFavoriteStatus.Size = new Size(76, 15);
            labelFavoriteStatus.TabIndex = 2;
            labelFavoriteStatus.Text = "お気に入り: 未";
            // 
            // labelBuildingDescription
            // 
            labelBuildingDescription.Location = new Point(12, 44);
            labelBuildingDescription.Name = "labelBuildingDescription";
            labelBuildingDescription.Size = new Size(159, 56);
            labelBuildingDescription.TabIndex = 1;
            labelBuildingDescription.Text = "説明";
            // 
            // labelBuildingTitle
            // 
            labelBuildingTitle.AutoSize = true;
            labelBuildingTitle.Location = new Point(12, 16);
            labelBuildingTitle.Name = "labelBuildingTitle";
            labelBuildingTitle.Size = new Size(43, 15);
            labelBuildingTitle.TabIndex = 0;
            labelBuildingTitle.Text = "建物名";
            // 
            // panelSearch
            // 
            panelSearch.Controls.Add(listBoxFavorites);
            panelSearch.Controls.Add(buttonFavoriteToGoal);
            panelSearch.Controls.Add(buttonFavoriteToStart);
            panelSearch.Controls.Add(listBoxGoal);
            panelSearch.Controls.Add(listBoxStart);
            panelSearch.Controls.Add(textBox2);
            panelSearch.Controls.Add(textBox1);
            panelSearch.Controls.Add(Button1);
            panelSearch.Dock = DockStyle.Fill;
            panelSearch.Location = new Point(0, 32);
            panelSearch.Name = "panelSearch";
            panelSearch.Size = new Size(182, 560);
            panelSearch.TabIndex = 8;
            // 
            // listBoxFavorites
            // 
            listBoxFavorites.FormattingEnabled = true;
            listBoxFavorites.IntegralHeight = false;
            listBoxFavorites.ItemHeight = 15;
            listBoxFavorites.Location = new Point(12, 252);
            listBoxFavorites.Name = "listBoxFavorites";
            listBoxFavorites.Size = new Size(159, 110);
            listBoxFavorites.TabIndex = 12;
            listBoxFavorites.Visible = false;
            listBoxFavorites.DoubleClick += OnFavoriteListDoubleClick;
            listBoxFavorites.KeyDown += OnFavoriteListKeyDown;
            // 
            // buttonFavoriteToGoal
            // 
            buttonFavoriteToGoal.Location = new Point(96, 226);
            buttonFavoriteToGoal.Name = "buttonFavoriteToGoal";
            buttonFavoriteToGoal.Size = new Size(75, 23);
            buttonFavoriteToGoal.TabIndex = 11;
            buttonFavoriteToGoal.Text = "目的に設定";
            buttonFavoriteToGoal.UseVisualStyleBackColor = true;
            buttonFavoriteToGoal.Click += OnFavoriteToGoalClick;
            // 
            // buttonFavoriteToStart
            // 
            buttonFavoriteToStart.Location = new Point(12, 226);
            buttonFavoriteToStart.Name = "buttonFavoriteToStart";
            buttonFavoriteToStart.Size = new Size(75, 23);
            buttonFavoriteToStart.TabIndex = 10;
            buttonFavoriteToStart.Text = "出発に設定";
            buttonFavoriteToStart.UseVisualStyleBackColor = true;
            buttonFavoriteToStart.Click += OnFavoriteToStartClick;
            // 
            // listBoxGoal
            // 
            listBoxGoal.FormattingEnabled = true;
            listBoxGoal.IntegralHeight = false;
            listBoxGoal.ItemHeight = 15;
            listBoxGoal.Location = new Point(12, 146);
            listBoxGoal.Name = "listBoxGoal";
            listBoxGoal.Size = new Size(159, 70);
            listBoxGoal.TabIndex = 9;
            listBoxGoal.Visible = false;
            listBoxGoal.DoubleClick += OnGoalSuggestionDoubleClick;
            listBoxGoal.KeyDown += OnSuggestionKeyDown;
            // 
            // listBoxStart
            // 
            listBoxStart.FormattingEnabled = true;
            listBoxStart.IntegralHeight = false;
            listBoxStart.ItemHeight = 15;
            listBoxStart.Location = new Point(12, 42);
            listBoxStart.Name = "listBoxStart";
            listBoxStart.Size = new Size(159, 70);
            listBoxStart.TabIndex = 7;
            listBoxStart.Visible = false;
            listBoxStart.DoubleClick += OnStartSuggestionDoubleClick;
            listBoxStart.KeyDown += OnSuggestionKeyDown;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(12, 120);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(159, 23);
            textBox2.TabIndex = 8;
            textBox2.TextChanged += OnGoalTextChanged;
            textBox2.Enter += OnSearchPanelRequested;
            textBox2.KeyDown += OnSearchTextBoxKeyDown;
            // 
            // panelTabs
            // 
            panelTabs.Controls.Add(buttonTabInfo);
            panelTabs.Controls.Add(buttonTabSearch);
            panelTabs.Dock = DockStyle.Top;
            panelTabs.Location = new Point(0, 0);
            panelTabs.Name = "panelTabs";
            panelTabs.Size = new Size(182, 32);
            panelTabs.TabIndex = 10;
            // 
            // buttonTabInfo
            // 
            buttonTabInfo.Dock = DockStyle.Right;
            buttonTabInfo.Location = new Point(91, 0);
            buttonTabInfo.Name = "buttonTabInfo";
            buttonTabInfo.Size = new Size(91, 32);
            buttonTabInfo.TabIndex = 1;
            buttonTabInfo.Text = "情報";
            buttonTabInfo.UseVisualStyleBackColor = true;
            buttonTabInfo.Click += OnInfoTabClick;
            // 
            // buttonTabSearch
            // 
            buttonTabSearch.Dock = DockStyle.Left;
            buttonTabSearch.Location = new Point(0, 0);
            buttonTabSearch.Name = "buttonTabSearch";
            buttonTabSearch.Size = new Size(91, 32);
            buttonTabSearch.TabIndex = 0;
            buttonTabSearch.Text = "検索";
            buttonTabSearch.UseVisualStyleBackColor = true;
            buttonTabSearch.Click += OnSearchTabClick;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(951, 616);
            Controls.Add(panel1);
            Controls.Add(MenuStrip1);
            Controls.Add(FlowLayoutPanel1);
            MainMenuStrip = MenuStrip1;
            Margin = new Padding(2, 1, 2, 1);
            Name = "Form1";
            Text = "Form1";
            Load += OnFormLoad;
            MenuStrip1.ResumeLayout(false);
            MenuStrip1.PerformLayout();
            panel1.ResumeLayout(false);
            panelBuilding.ResumeLayout(false);
            panelBuilding.PerformLayout();
            panelSearch.ResumeLayout(false);
            panelSearch.PerformLayout();
            panelTabs.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button Button1;
        private FlowLayoutPanel FlowLayoutPanel1;
        private MenuStrip MenuStrip1;
        private ToolStripMenuItem MenuItem_General;
        private ToolStripMenuItem MenuItem_Senmon;
        private ToolStripMenuItem MenuItem_Kougi;
        private ToolStripMenuItem MenuItem_Advanced;
        private ToolStripMenuItem MenuItem_Honbu;
        private ToolStripMenuItem MenuItem_Elec;
        private ToolStripMenuItem MenuItem_Denshi;
        private ToolStripMenuItem MenuItem_Toshi;
        private ToolStripMenuItem MenuItem_Mech;
        private ToolStripMenuItem MenuItem_Chem;
        private ToolStripMenuItem MenuItem_General_1F;
        private ToolStripMenuItem MenuItem_General_2F;
        private ToolStripMenuItem MenuItem_General_3F;
        private ToolStripMenuItem MenuItem_General_4F;
        private ToolStripMenuItem MenuItem_General_5F;
        private ToolStripMenuItem MenuItem_General_6F;
        private ToolStripMenuItem MenuItem_Elec_1F;
        private ToolStripMenuItem MenuItem_Elec_2F;
        private ToolStripMenuItem MenuItem_Elec_3F;
        private ToolStripMenuItem MenuItem_Elec_4F;
        private ToolStripMenuItem MenuItem_Denshi_1F;
        private ToolStripMenuItem MenuItem_Denshi_2F;
        private ToolStripMenuItem MenuItem_Denshi_3F;
        private ToolStripMenuItem MenuItem_Denshi_4F;
        private ToolStripMenuItem MenuItem_Toshi_1F;
        private ToolStripMenuItem MenuItem_Toshi_2F;
        private ToolStripMenuItem MenuItem_Toshi_3F;
        private ToolStripMenuItem MenuItem_Toshi_4F;
        private ToolStripMenuItem MenuItem_Mech_1F;
        private ToolStripMenuItem MenuItem_Mech_2F;
        private ToolStripMenuItem MenuItem_Mech_3F;
        private ToolStripMenuItem MenuItem_Mech_4F;
        private ToolStripMenuItem MenuItem_Chem_F1;
        private ToolStripMenuItem MenuItem_Chem_2F;
        private ToolStripMenuItem MenuItem_Chem_3F;
        private ToolStripMenuItem MenuItem_Chem_4F;
        private ToolStripMenuItem MenuItem_Advanced_2F;
        private ToolStripMenuItem MenuItem_Advanced_3F;
        private ToolStripMenuItem MenuItem_Honbu_1F;
        private ToolStripMenuItem MenuItem_Honbu_2F;
        private ToolStripMenuItem MenuItem_Kougi_1F;
        private ToolStripMenuItem MenuItem_Kougi_2F;
        private ToolStripMenuItem MenuItem_Kougi_3F;
        private ToolStripMenuItem MenuItem_Gakusei;
        private ToolStripMenuItem MenuItem_Kougi_4F;
        private ToolStripMenuItem MenuItem_Gakusei_1F;
        private ToolStripMenuItem MenuItem_Gakusei_2F;
        private ToolStripMenuItem MenuItem_Gakusei_3F;
        private ToolStripMenuItem MenuItem_Library;
        private ToolStripMenuItem MenuItem_Library_1F;
        private ToolStripMenuItem MenuItem_Library_2F;
        private ToolStripMenuItem MenuItem_Advanced_1F;
        private TextBox textBox1;
        private Panel panel1;
        private Panel panelSearch;
        private Panel panelBuilding;
        private Panel panelTabs;
        private TextBox textBox2;
        private ListBox listBoxStart;
        private ListBox listBoxGoal;
        private ListBox listBoxFavorites;
        private Label labelBuildingTitle;
        private Label labelBuildingDescription;
        private Label labelFavoriteStatus;
        private Button buttonFavorite;
        private Button buttonUnfavorite;
        private Button buttonTabSearch;
        private Button buttonTabInfo;
        private Button buttonFavoriteToStart;
        private Button buttonFavoriteToGoal;
    }
}
