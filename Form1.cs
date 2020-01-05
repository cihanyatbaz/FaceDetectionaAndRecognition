using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;

namespace FaceDetectionaAndRecognition
{

    public partial class Form1 : Form
    {
        //Declare Variables to use them in all this project

        MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_TRIPLEX, 0.6, 0.6d);
        HaarCascade faceDetected;
        Image<Bgr, Byte> Frame;  // Emgu.CV.Structure
        Capture camera;
        Image<Gray, byte> result;
        Image<Gray, byte> TrainedFace = null;
        Image<Gray, byte> grayFace = null;
        List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        List<string> labels = new List<string>();
        List<string> Users = new List<string>();
        int Count, NumLables;
        string name, names = null;


        // Database connections
        OleDbCommand command;
        OleDbDataReader dataread;
        OleDbConnection connect;
        
        public Form1()
        {
            InitializeComponent();

            // Here we need to put the haarcascade xml file
            // HaarCascade is for face detection 
            faceDetected = new HaarCascade("haarcascade_frontalface_default.xml");


            // Database Connections
            connect = new OleDbConnection("Provider=Microsoft.JET.OleDb.4.0;Data Source=database.mdb");
            connect.Open();
            command = new OleDbCommand("Select ID,name,picture from informations", connect);
            dataread = command.ExecuteReader();


            try
            {


                // Here, it is reading all faces 

                while (dataread.Read())
                {

                    NumLables = Convert.ToInt32(dataread["ID"]);
                    Count = NumLables;

                    string FacesLoad = Convert.ToString(dataread["picture"].ToString().Substring(99, 11));
                    // Here we going to load to the Faces
                    //FacesLoad = "face" + i + ".bmp";
                    trainingImages.Add(new Image<Gray, byte>(Application.StartupPath + $"/Faces/{FacesLoad}"));

                }
                connect.Close();
                
            }
            catch (Exception)
            {
                
            }
        }






        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
               
                // Data connections for the save new face
                connect = new OleDbConnection("Provider=Microsoft.JET.OleDb.4.0;Data Source=database.mdb");
                connect.Open();
                command = new OleDbCommand("Select ID,name,picture from informations", connect);
                dataread = command.ExecuteReader();


                // Here, it will load and recognition all faces which are saved before
                int i = 0;
                while (dataread.Read())
                {

                    string FacesLoad = Convert.ToString(dataread["picture"].ToString());
                                      
                    trainingImages.Add(new Image<Gray, byte>(FacesLoad));
                    
                    trainingImages.ToArray()[i].Save(dataread["picture"].ToString());
                    labels.Add(dataread["name"].ToString());
                    i++;
                    
                }
                connect.Close();
                

                // it is going to open camera
                camera = new Capture();
                camera.QueryFrame();
                Application.Idle += new EventHandler(FrameProcedure);
            }
            catch (NullReferenceException ex)
            {   // Show errors if there are any problem
                MessageBox.Show(ex.Message);
            }
        }

        // Sace the faces
        private void saveButton_Click(object sender, EventArgs e)
        {
            Count = Count + 1;
            grayFace = camera.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            MCvAvgComp[][] DetectedFaces = grayFace.DetectHaarCascade(faceDetected, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));

            foreach (MCvAvgComp f in DetectedFaces[0])
            {
                TrainedFace = Frame.Copy(f.rect).Convert<Gray, Byte>();
                break;
            }
            string imagePath = "";
            connect = new OleDbConnection("Provider=Microsoft.JET.OleDb.4.0;Data Source=database.mdb");
          
            string query = "Insert into informations (name,picture) values (@name, @picture)";
            command = new OleDbCommand(query, connect);

           

            Random ran = new Random();

            TrainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            trainingImages.Add(TrainedFace);
            labels.Add(textName.Text);

            string x = DateTime.Now.Millisecond.ToString();
            x = Convert.ToString(Convert.ToInt32(x) * ran.Next(1, 10000));

            imagePath = Application.StartupPath + "/Faces/face" + x + ".bmp";

            for (int i = 1; i < trainingImages.ToArray().Length + 1; i++)
            {

                trainingImages.ToArray()[i-1].Save(imagePath);

            }

            command.Parameters.AddWithValue("@name", textName.Text);
            command.Parameters.AddWithValue("@picture", imagePath);
 
            connect.Open();
            command.ExecuteNonQuery();

            connect.Close();
            MessageBox.Show(textName.Text + " Added Successfully");
        }

        private void FrameProcedure(object sender, EventArgs e)
        {
            // DETECT THE FACE
            Users.Add("");
            Frame = camera.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            grayFace = Frame.Convert<Gray, Byte>();
            MCvAvgComp[][] facesDetectedNow = grayFace.DetectHaarCascade(faceDetected, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));

            foreach (MCvAvgComp f in facesDetectedNow[0])
            {
                result = Frame.Copy(f.rect).Convert<Gray, Byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                Frame.Draw(f.rect, new Bgr(Color.Green), 3);
                if (trainingImages.ToArray().Length != 0)
                {
                    MCvTermCriteria termCriteria = new MCvTermCriteria(Count, 0.001);
                    EigenObjectRecognizer recognizer = new EigenObjectRecognizer(trainingImages.ToArray(), labels.ToArray(), 1500, ref termCriteria);
                    name = recognizer.Recognize(result);
                    Frame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.Red));

                }

                Users.Add("");
            }
            cameraBox.Image = Frame;
            names = "";
            Users.Clear();

        }
    }
}
