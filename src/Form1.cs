namespace team1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void 一般科棟ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }



        private void 電気工学科棟ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }




        private void ChangeImage(string path) //function for change image
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }

            pictureBox1.Image = Image.FromFile(path);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private void fToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeImage(@"Y:\team1\src\data\ippan_1.png");
        }

        private void fToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ChangeImage(@"Y:\team1\src\data\ippan_2.png");
        }

        private void fToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            ChangeImage(@"Y:\team1\src\data\ippan_3.png");
        }

        private void fToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            ChangeImage(@"Y:\team1\src\data\ippan_4.png");
        }

        private void fToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            ChangeImage(@"Y:\team1\src\data\ippan_5.png");
        }

        private void fToolStripMenuItem5_Click(object sender, EventArgs e)
        {
            ChangeImage(@"Y:\team1\src\data\ippan_6.png");
        }

        private void fToolStripMenuItem9_Click(object sender, EventArgs e)
        {
            ChangeImage(@"Y:\team1\src\data\denki1.png");
        }

        private void fToolStripMenuItem10_Click(object sender, EventArgs e)
        {
            ChangeImage(@"Y:\team1\src\data\denki2.png");
        }

        private void fToolStripMenuItem11_Click(object sender, EventArgs e)
        {
            ChangeImage(@"Y:\team1\src\data\denki3.png");
        }

        private void fToolStripMenuItem12_Click(object sender, EventArgs e)
        {
            ChangeImage(@"Y:\team1\src\data\denki4.png");
        }

        private void fToolStripMenuItem6_Click(object sender, EventArgs e)
        {
            ChangeImage(@"Y:\team1\src\data\densi1.png");
        }

        private void fToolStripMenuItem7_Click(object sender, EventArgs e)
        {
            ChangeImage(@"Y:\team1\src\data\densi2.png");
        }

        private void ｆToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeImage(@"Y:\team1\src\data\densi3.png");
        }

        private void fToolStripMenuItem8_Click(object sender, EventArgs e)
        {
            ChangeImage(@"Y:\team1\src\data\densi4.png");
        }
    }
}
