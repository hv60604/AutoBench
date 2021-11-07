using System;
using System.Diagnostics;
using System.Management;
//using System.Data.Linq;
//using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ScriptPortal.Vegas;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace hv_bench
{
    public partial class Form1 : Form
    {
        public Vegas myVegas;
        private Renderer[] FullRenderer = new Renderer[999];
        private RenderTemplate[] FullTemplate = new RenderTemplate[999];

        private bool GotFileNames = false;
        private string[] fileNames;
        private string filelist;
        private string initRenderDir="";
        private string initProjDir="";
        private string csvVersion;
        private string csvCpu;
        private string csvCores;
        private string csvGpu;
        private string csvIgpu;
        private string csvDecoder;
        private string csvRender;
        private string csvEncode;
        private string csvFrame;
        private string csvSampProj;
        private string csvRedCar;
        private string csvRedCar4K;
        private string csvRedCarHevc;
        private string csvRedCarProRes;
        private int fileCount;
        private string[] GraphicsCards;
        private string tmpTxt;
        private string myCpu;
        public int sleepCount = 500;   // to 1000 for 15 second pause between renders
        private bool exit;
        private bool running;
        private bool dryrun;
        TimeSpan timeTaken;
        string csvFile = "";
        string dateNow = DateTime.UtcNow.ToLocalTime().ToString("yy-MM-dd HH\uFF1Amm");
        string build = Properties.Resources.BuildDate;


        public Form1(Vegas vegas)
        {

            myVegas = vegas;

            InitializeComponent();
            //           this.Text = "AutoBench build(" + dateNow + ")";
 
            FindAllRenderers();

            FindAllGraphicsCards();

            //FindGpuDrivers();

            myCpu = GetCpu();
            csvCores = GetCores();
            SleepAmt.Value = sleepCount / 50;

            // default init form fields for output renders and projects
            initRenderDir = "D:\\temp\\";
            initProjDir = "D:\\MISC\\Benchmarks\\";

            // override defaults from AutoBench.txt file
            GetCfg();

            txtDirBox.Text = initRenderDir;
            txtCpu.Text = myCpu;
            txtProjDir.Text = initProjDir;

            // unlock dll on script exit to allow re-compile during development
            // might reload faster without this
            myVegas.UnloadScriptDomainOnScriptExit = true;

        }

        public void GetCfg()
        {
            // look in same place that dll loads from
            string fqfn;
            fqfn = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\AutoBench.txt";
 
            if (File.Exists(fqfn)) {
                var enumLines = File.ReadLines(fqfn, Encoding.UTF8);

                foreach (var line in enumLines)
                {
                    if (!line.Trim().StartsWith("//")) {
                        if (line.Contains("RenderDir="))
                        { initRenderDir = line.Substring(line.IndexOf('=') + 1).Trim(); }

                        if (line.Contains("ProjDir="))
                        { initProjDir = line.Substring(line.IndexOf('=') + 1).Trim(); }

                        if (line.Contains("CPU="))
                        { myCpu = line.Substring(line.IndexOf('=') + 1).Trim(); }

                        //update 10/1/21
                        //if (line.Contains("GraphicsCard=") &&  line.Contains("other"))
                        //{ cmbGraphicsCard.SelectedIndex = 0; }

                        //if (line.Contains("Decoder=") &&  line.Contains("none"))
                        //{ cmbDecoder.SelectedIndex = 0; }

                        //if (line.Contains("Decoder=") && line.Contains("other"))
                        //{ cmbDecoder.SelectedIndex = GraphicsCards.Length; }
                        if (line.Contains("GPU="))
                        //{ cmbGraphicsCard.Text = line.Substring(line.IndexOf('=') + 1).Trim(); }
                        //{ cmbDecoder.SelectedIndex = 1; cmbGraphicsCard.SelectedText = line.Substring(line.IndexOf('=') + 1).Trim(); }
                        { cmbGraphicsCard.Items.Add( line.Substring(line.IndexOf('=') + 1).Trim() );
                          cmbGraphicsCard.SelectedIndex = cmbGraphicsCard.Items.Count - 1 ;
                        }

                        if (line.Contains("Decoder="))
                        { cmbDecoder.Items.Add (cmbDecoder.SelectedText = line.Substring(line.IndexOf('=') + 1).Trim());
                          cmbDecoder.SelectedIndex = cmbDecoder.Items.Count - 1 ;
                        }

                        if (line.Contains("Sleep="))
                        { 
                            sleepCount = Convert.ToInt32(line.Substring(line.IndexOf('=') + 1).Trim());
                            SleepAmt.Value = sleepCount / 50;
                        }
                        
                    }

                }
            }
        }

        public void FindGpuDrivers()
        {
            using (var searcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    MessageBox.Show("Name  -  " + obj["Name"] +
                    //"\nDeviceID  -  " + obj["DeviceID"] + 
                    //"\nAdapterRAM  -  " + obj["AdapterRAM"] +
                    //"\nAdapterDACType  -  " + obj["AdapterDACType"] +
                    //"\nMonochrome  -  " + obj["Monochrome"] +

                    "\n\nCaption  -  " + obj["Caption"] +
                    "\n\nDriverVersion  -  " + obj["DriverVersion"]);
                    //"\nVideoProcessor  -  " + obj["VideoProcessor"] +
                    //"\nVideoArchitecture  -  " + obj["VideoArchitecture"] +
                    //"\n\nVideoMemoryType  -  " + obj["VideoMemoryType"] );
                }
            }

        }
        
        public void FindAllGraphicsCards()
        {
            // find graphic card names
            //            System.Text.Encoding utf_8 = System.Text.Encoding.UTF8;
            string[] Decoder;

            List<string> list = new List<string>();
            using (var searcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    list.Add((String)obj["Name"]);
                }
            }
            GraphicsCards = list.ToArray();

            Decoder = list.ToArray();

            // allow for selecting no decoder
            cmbDecoder.Items.Add("none");

            foreach (String card in GraphicsCards)
            {
                //byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(card);
                //string tmpStr = System.Text.Encoding.UTF8.GetString(utf8Bytes);
                //cmbGraphicsCard.Items.Add(tmpStr);
                //cmbDecoder.Items.Add(tmpStr);
                cmbGraphicsCard.Items.Add(card);
                cmbDecoder.Items.Add(card);
            }

            // default gpu to the last graphics card and decoder to first
            cmbGraphicsCard.SelectedIndex = GraphicsCards.Length-1;
            cmbDecoder.SelectedIndex = 1;
        }

        public static String GetCpu()
        {
            using (ManagementObjectSearcher win32Proc = new ManagementObjectSearcher("select * from Win32_Processor"),
                win32CompSys = new ManagementObjectSearcher("select * from Win32_ComputerSystem"),
                win32Memory = new ManagementObjectSearcher("select * from Win32_PhysicalMemory"))
            {
                String procName = "";
                foreach (ManagementObject obj in win32Proc.Get())
                {
                    procName = obj["Name"].ToString();
                }
                return procName;
            }
        }

        public static String GetCores()
        {
            int coreCount = 0;
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                coreCount += int.Parse(item["NumberOfCores"].ToString());
            }
            return coreCount.ToString();
         }

        public void hideCfg()
        {
            SleepAdjust.Visible = false;
            SleepAmt.Visible = false;
            cbHideCfg.Visible = false;
        }

        
        public void FindAllRenderers()
        {
            int RCount = 0; 
            int myRcount1 = 0; int myRcount2 = 0; int myRcount3 = 0; int myRcount4 = 0; int myRcount5 = 0; int myRcount6 = 0;
            int myScount1 = 0; int myScount2 = 0; int myScount3 = 0; int myScount4 = 0; int myScount5 = 0; int myScount6 = 0;
            string tmpT;

            try
            {
                foreach (Renderer renderer in myVegas.Renderers)
                {
                    try
                    {
                        foreach (RenderTemplate renderTemplate in renderer.Templates)
                        {
                            if (renderTemplate.IsValid())
                            {
                                FullRenderer[RCount] = renderer;
                                FullTemplate[RCount] = renderTemplate;
                                cmbRenderType.Items.Add(renderer.Name + " - " + renderTemplate.Name);
                                cmbRedCarHD2.Items.Add(renderer.Name + " - " + renderTemplate.Name);
                                cmbRedCarHD3.Items.Add(renderer.Name + " - " + renderTemplate.Name);
                                cmbRedCar4K1.Items.Add(renderer.Name + " - " + renderTemplate.Name);
                                cmbRedCar4K2.Items.Add(renderer.Name + " - " + renderTemplate.Name);
                                cmbRedCar4K3.Items.Add(renderer.Name + " - " + renderTemplate.Name);

                                cmbSampProjHD1.Items.Add(renderer.Name + " - " + renderTemplate.Name);
                                cmbSampProjHD2.Items.Add(renderer.Name + " - " + renderTemplate.Name);
                                cmbSampProjHD3.Items.Add(renderer.Name + " - " + renderTemplate.Name);
                                cmbSampProj4K1.Items.Add(renderer.Name + " - " + renderTemplate.Name);
                                cmbSampProj4K2.Items.Add(renderer.Name + " - " + renderTemplate.Name);
                                cmbSampProj4K3.Items.Add(renderer.Name + " - " + renderTemplate.Name);

                                tmpT = renderer.Name + " - " + renderTemplate.Name;
 /*                               if (false)
                                {
                                    if (tmpT.Contains("RedCar HD NVenc)") || tmpT.Contains("RedCar HD VCE)")) { myRcount1 = RCount; }
                                        else if (tmpT.Contains("RedCar HD QSV)") || tmpT.Contains("RedCar HD NVenc)")) { myRcount2 = RCount; }
                                        else if (tmpT.Contains("RedCar HD MC)")) { myRcount3 = RCount; }
                                        else if (tmpT.Contains("(RedCar 4K NVenc)") || tmpT.Contains("(RedCar 4K VCE)")) { myRcount4 = RCount; }
                                        else if (tmpT.Contains("RedCar 4K QSV)") || tmpT.Contains("RedCar 4K NVenc)")) { myRcount5 = RCount; }
                                        else if (tmpT.Contains("RedCar 4K MC)")) { myRcount6 = RCount; }
                                        else if (tmpT.Contains("SampProj HD NVenc)") || tmpT.Contains("SampProj HD VCE)")) { myScount1 = RCount; }
                                        else if (tmpT.Contains("SampProj HD QSV)") || tmpT.Contains("SampProj HD NVenc)")) { myScount2 = RCount; }
                                        else if (tmpT.Contains("(SampProj HD MC)")) { myScount3 = RCount; }
                                        else if (tmpT.Contains("SampProj 4K NVenc)") || tmpT.Contains("SampProj 4K VCE)")) { myScount4 = RCount; }
                                        else if (tmpT.Contains("SampProj 4K QSV)") || tmpT.Contains("SampProj 4K NVenc)")) { myScount5 = RCount; }
                                        else if (tmpT.Contains("SampProj 4K MC")) { myScount6 = RCount; }
                                    } else  {
*/
                                        if (tmpT.Contains("RedCar HD NVenc)") || tmpT.Contains("RedCar HD VCE)")) { myRcount1 = RCount; }
                                        if (tmpT.Contains("RedCar HD QSV)") || tmpT.Contains("RedCar HD NVenc)")) { myRcount2 = RCount; }
                                        if (tmpT.Contains("RedCar HD MC)")) { myRcount3 = RCount; }
                                        if (tmpT.Contains("(RedCar 4K NVenc)") || tmpT.Contains("(RedCar 4K VCE)")) { myRcount4 = RCount; }
                                        if (tmpT.Contains("RedCar 4K QSV)") || tmpT.Contains("RedCar 4K NVenc)")) { myRcount5 = RCount; }
                                        if (tmpT.Contains("RedCar 4K MC)")) { myRcount6 = RCount; }
                                        if (tmpT.Contains("SampProj HD NVenc)") || tmpT.Contains("SampProj HD VCE)")) { myScount1 = RCount; }
                                        if (tmpT.Contains("SampProj HD QSV)") || tmpT.Contains("SampProj HD NVenc)")) { myScount2 = RCount; }
                                        if (tmpT.Contains("(SampProj HD MC)")) { myScount3 = RCount; }
                                        if (tmpT.Contains("SampProj 4K NVenc)") || tmpT.Contains("SampProj 4K VCE)")) { myScount4 = RCount; }
                                        if (tmpT.Contains("SampProj 4K QSV)") || tmpT.Contains("SampProj 4K NVenc)")) { myScount5 = RCount; }
                                        if (tmpT.Contains("SampProj 4K MC")) { myScount6 = RCount; }

//                                    }
                                RCount++;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // sets to my defauls in form
            cmbRenderType.SelectedIndex = myRcount1;
            if (cmbRenderType.SelectedIndex < 0)
            {   cmbRenderType.SelectedIndex = 0; }

            cmbRedCarHD2.SelectedIndex = myRcount2;
            if (cmbRedCarHD2.SelectedIndex < 0)
            { cmbRedCarHD2.SelectedIndex = 0; }

            cmbRedCarHD3.SelectedIndex = myRcount3;
            if (cmbRedCarHD3.SelectedIndex < 0)
            { cmbRedCarHD3.SelectedIndex = 0; }

            cmbRedCar4K1.SelectedIndex = myRcount4;
            if (cmbRedCar4K1.SelectedIndex < 0)
            { cmbRedCar4K1.SelectedIndex = 0; }

            cmbRedCar4K2.SelectedIndex = myRcount5;
            if (cmbRedCar4K2.SelectedIndex < 0)
            { cmbRedCar4K2.SelectedIndex = 0; }

            cmbRedCar4K3.SelectedIndex = myRcount6;
            if (cmbRedCar4K3.SelectedIndex < 0)
            { cmbRedCar4K3.SelectedIndex = 0; }

            cmbSampProjHD1.SelectedIndex = myScount1;
            if (cmbSampProjHD1.SelectedIndex < 0)
            { cmbSampProjHD1.SelectedIndex = 0; }

            cmbSampProjHD2.SelectedIndex = myScount2;
            if (cmbRedCarHD2.SelectedIndex < 0)
            { cmbRedCarHD2.SelectedIndex = 0; }

            cmbSampProjHD3.SelectedIndex = myScount3;
            if (cmbRedCarHD3.SelectedIndex < 0)
            { cmbRedCarHD3.SelectedIndex = 0; }

            cmbSampProj4K1.SelectedIndex = myScount4;
            if (cmbSampProj4K1.SelectedIndex < 0)
            { cmbSampProj4K1.SelectedIndex = 0; }

            cmbSampProj4K2.SelectedIndex = myScount5;
            if (cmbSampProj4K2.SelectedIndex < 0)
            { cmbSampProj4K2.SelectedIndex = 0; }

            cmbSampProj4K3.SelectedIndex = myScount6;
            if (cmbSampProj4K3.SelectedIndex < 0)
            { cmbSampProj4K3.SelectedIndex = 0; }


        }

        public string GetSaveDir(string OrgSaveDir)
        {
            if (OrgSaveDir == null)
            {
                OrgSaveDir = "";
            }
            FolderBrowserDialog saveFileDialog = new FolderBrowserDialog();
            saveFileDialog.Description = "Select the desired folder";
            saveFileDialog.ShowNewFolderButton = true;
            if (!(OrgSaveDir == ""))
            {
                string initialDir = OrgSaveDir;
                if (Directory.Exists(initialDir))
                {
                    saveFileDialog.SelectedPath = initialDir;
                }
            }
            else
            {
                //                saveFileDialog.SelectedPath = "c:\\";
                saveFileDialog.SelectedPath = "d:\\";
            }
            if (System.Windows.Forms.DialogResult.OK == saveFileDialog.ShowDialog())
            {
                return Path.GetFullPath(saveFileDialog.SelectedPath) + Path.DirectorySeparatorChar;
            }
            else
            {
                return OrgSaveDir;
            }
        }


        public void wait(int ms)
        {
            DateTime datetemp = DateTime.Now;
            do
            {
                Application.DoEvents();
            } while (datetemp.AddMilliseconds(ms) > DateTime.Now);
        }


        private void btnRender_Click(object sender, EventArgs e)
        {
            Renderer myRenderer = FullRenderer[cmbRenderType.SelectedIndex];
            RenderTemplate myTemplate = FullTemplate[cmbRenderType.SelectedIndex];

            hideCfg();



            if (running)
            {
                btnRender.Text = "Cancel Pending";
                btnRender.Enabled = false;
                exit = true;
                btnRender.Refresh();

                return;
            }

            if (btnRender.Text == "Render") exit = false;

            if (btnRender.Text == "Cancel") { 
                btnRender.Text = "Render"; 
                mymsg.Text = "User Cancelled.";
                exit = true;
                return; }

                if (!GotFileNames)
            {
                MessageBox.Show("You must first select projects to render. Please press the 'Select Projects' button.");
                return;
            }
            string drive = Path.GetPathRoot(txtDirBox.Text);
            if (!Directory.Exists(drive))
            {
                MessageBox.Show("Save Directory:  " + drive + " is not found or is inaccessible.  Please choose another.",
                                "Error");
                return;
            }

            if (MessageBox.Show("Ready to Benchmark... continue?", "Render Now", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                exit = true;
                dryrun = false;
                //running = false;
                btnRender.Text = "Render";
                btnRender.Refresh();
                return;
            }
            else
            {
                dryrun = true;
                exit = false;
                fileCount = getFileCount();
                if (fileCount==0)
                {
                    MessageBox.Show("No Projects were found matching the render templates selected.\n");
                    exit = true;
                    //running = false;
                    dryrun = false;
                    return;
                }
                dryrun = false;
                btnRender.Text = "Cancel";
                btnRender.Refresh();
            }

            tmpTxt = myVegas.Version;
            csvVersion = tmpTxt.Substring(8, 3) + tmpTxt.Substring(20, 3);

            // extract the word before the word CPU except if a Xeon, then word after CPU only if myCpu has not been shortened on form or config
            if (myCpu.Length > 10)
            {
                int endpoint;
                endpoint = myCpu.IndexOf(" CPU");
                if (endpoint < 0) endpoint = myCpu.Length - 1;

                if (!myCpu.Contains("Xeon")) { csvCpu = myCpu.Substring(endpoint - 8, 7); }
                else { csvCpu = "Xeon " + Regex.Replace(myCpu.Substring(endpoint + 8, 7), @"\s+", ""); }
            }
            else csvCpu = myCpu;

            //update 10/1/21
            //tmpTxt = (string)cmbGraphicsCard.Text;
            //if (tmpTxt.Contains("1050 Ti")) { csvGpu = "1050ti"; }
            //else if (tmpTxt.Contains("5700")) { csvGpu = "5700xt"; }
            //else if (tmpTxt.Contains("7")) { csvGpu = "Radeon7"; }
            //else if (tmpTxt.Contains("64")) { csvGpu = "Vega 64"; }
            //else if (tmpTxt.Contains("Vega")) { csvGpu = "Vega M"; }
            //else if (tmpTxt.Contains("1660")) { csvGpu = "gtx1660"; }
            //else if (tmpTxt.ToString().Contains("630")) { csvGpu = "uhd630"; }
            //else { csvGpu = cmbGraphicsCard.Text; }
            csvGpu = cmbGraphicsCard.Text;

            tmpTxt = (string)cmbDecoder.Text.ToLower();
            //if (tmpTxt.Contains("630")) { csvIgpu = "UHD630"; csvDecoder = "QSV"; }
            //else if (tmpTxt.Contains("1660")) { csvIgpu = "gtx1660"; csvDecoder = "NVdec"; }
            //else if (tmpTxt.Contains("1050")) { csvIgpu = "gtx1050ti"; csvDecoder = "NVdec"; }
            //else if (tmpTxt.Contains("5700")) { csvIgpu = "5700xt"; csvDecoder = "VCE"; }
            //else if (tmpTxt.Contains("7")) { csvIgpu = "Radeon7"; csvDecoder = "VCE"; }
            //else { csvIgpu = cmbDecoder.Text; csvDecoder = ""; }
            csvIgpu = cmbDecoder.Text;
            if (tmpTxt.Contains("uhd")) { csvDecoder = "QSV"; }
             else if (tmpTxt.Contains("1660")) { csvDecoder = "NVdec"; }
             else if (tmpTxt.Contains("1050")) { csvDecoder = "NVdec"; }
             else if (tmpTxt.Contains("5700")) { csvDecoder = "VCE"; }
             else if (tmpTxt.Contains("vii")) { csvDecoder = "VCE"; }
             else { csvDecoder = ""; }

            // initialize the sleep-between-renders progress bar
            //progressBar1.Minimum = 0;
            //progressBar1.Maximum = sleepCount;
            //progressBar1.Step = 1;
            //progressBar1.Value = 0;
            //progressBar1.Visible = false;
            //this.progressBar1.Refresh();


            bool FHDchecked = cbSampProjHD1.Checked || cbSampProjHD2.Checked || cbSampProjHD3.Checked ||
                                   cbRedCarHD1.Checked || cbRedCarHD2.Checked || cbRedCarHD3.Checked;

            bool UHDchecked = cbSampProj4K1.Checked || cbSampProj4K2.Checked || cbSampProj4K3.Checked ||
                                cbRedCar4K1.Checked || cbRedCar4K2.Checked || cbRedCar4K3.Checked;

            // append to or create a csv file to log elapsed times
            csvFile = "render time " + dateNow + " (" + csvCpu + " v" + csvVersion + ").csv";

            //MessageBox.Show("creating csvFile: " + csvFile);

            File.AppendAllText(txtDirBox.Text + csvFile,
                    "version,cpu,cores,gpu,igpu,frame,render as,encode,decode,SampProj,RedCar,RedCar4K,RedCarHevc,RedCarProRes");

            string[] frames = { "FHD", "UHD" };
            foreach (string frame in frames)
            {

                if (frame == "FHD" && FHDchecked) renderProjects(myRenderer, myTemplate, frame);
                if (frame == "UHD" && UHDchecked) renderProjects(myRenderer, myTemplate, frame);
                if (exit) return;
            }
        }

        private int getFileCount()
        {
            Renderer myRenderer = FullRenderer[cmbRenderType.SelectedIndex];
            RenderTemplate myTemplate = FullTemplate[cmbRenderType.SelectedIndex];

            //            fileCount = -10;

            // init for dry run count up
            fileCount = 0;
            string[] frames = { "FHD", "UHD" };
            foreach (string frame in frames)
            {
                renderProjects(myRenderer, myTemplate, frame);
            }
            return fileCount;
        }

        private void renderProjects(Renderer myRenderer, RenderTemplate myTemplate, string frame)
        {
            // assuming 2 graphics cards or 1 graphic + 1 igpu
            // (needs work if only 1 graphics card) 
            int bestOfSeconds;
            TimeSpan bestOfTimes;
            string foo;
            TimeSpan thisTimeTaken;
            Timecode RStart; 
            Timecode RLength;
            bool framestuff=false;

            int[] encodes = { 1, 2, 3 };
            foreach (int encode in encodes)
            {
                if (exit) { return; }

                // init csv output fields for each encode
                csvSampProj = "";
                csvRedCar = "";
                csvRedCar4K = "";
                csvRedCarHevc = "";
                csvRedCarProRes = "";

                if (!dryrun)
                {
                    // output begining of csv line ... the rest will be filled in after the reps of each project file
                    File.AppendAllText(txtDirBox.Text + csvFile,
                        "\n" + csvVersion + "," +
                        csvCpu + "," +
                        csvCores + "," +
                        csvGpu + "," +
                        csvIgpu);

                    // flag that frame-stuff not done yet
                    framestuff = false;
                }


                // Read the files, set render params for each, and render them all
                foreach (String file in fileNames)
                {
                    //if (exit) { running = false; return; }
                    if (exit) { return; }

                    string FullFileName = txtDirBox.Text + Path.GetFileNameWithoutExtension(file) + myRenderer.FileExtension.Substring(1, 4);

   

                    if (frame == "FHD")
                    {
                        if (encode == 1)
                        {
                            if (file.Contains("SampleProj"))
                            {
                                if (!cbSampProjHD1.Checked) { continue; }
                                myRenderer = FullRenderer[cmbSampProjHD1.SelectedIndex];
                                myTemplate = FullTemplate[cmbSampProjHD1.SelectedIndex];

                            }
                            else
                            {
                                if (!cbRedCarHD1.Checked) { continue; }
                                myRenderer = FullRenderer[cmbRenderType.SelectedIndex];
                                myTemplate = FullTemplate[cmbRenderType.SelectedIndex];
                            }
                        }
                        if (encode == 2)
                        {
                            if (file.Contains("SampleProj"))
                            {
                                if (!cbSampProjHD2.Checked) { continue; }
                                myRenderer = FullRenderer[cmbSampProjHD2.SelectedIndex];
                                myTemplate = FullTemplate[cmbSampProjHD2.SelectedIndex];
                            }
                            else
                            {
                                if (!cbRedCarHD2.Checked) { continue; }
                                myRenderer = FullRenderer[cmbRedCarHD2.SelectedIndex];
                                myTemplate = FullTemplate[cmbRedCarHD2.SelectedIndex];
                            }
                        }
                        if (encode == 3)
                        {
                            if (file.Contains("SampleProj"))
                            {
                                if (!cbSampProjHD3.Checked) { continue; }
                                myRenderer = FullRenderer[cmbSampProjHD3.SelectedIndex];
                                myTemplate = FullTemplate[cmbSampProjHD3.SelectedIndex];
                            }
                            else
                            {
                                if (!cbRedCarHD3.Checked) { continue; }
                                myRenderer = FullRenderer[cmbRedCarHD3.SelectedIndex];
                                myTemplate = FullTemplate[cmbRedCarHD3.SelectedIndex];
                            }
                        }
                    }
                    
                    if (frame == "UHD")
                    {
                        if (encode == 1)
                        {
                            if (file.Contains("SampleProj"))
                            {
                                if (!cbSampProj4K1.Checked) { continue; }
                                myRenderer = FullRenderer[cmbSampProj4K1.SelectedIndex];
                                myTemplate = FullTemplate[cmbSampProj4K1.SelectedIndex];
                            }
                            else
                            {
                                if (!cbRedCar4K1.Checked) { continue; }
                                myRenderer = FullRenderer[cmbRedCar4K1.SelectedIndex];
                                myTemplate = FullTemplate[cmbRedCar4K1.SelectedIndex];
                            }
                        }
                        if (encode == 2)
                        {
                            if (file.Contains("SampleProj"))
                            {
                                if (!cbSampProj4K2.Checked) { continue; }
                                myRenderer = FullRenderer[cmbSampProj4K2.SelectedIndex];
                                myTemplate = FullTemplate[cmbSampProj4K2.SelectedIndex];
                            }
                            else
                            {
                                if (!cbRedCar4K2.Checked) { continue; }
                                myRenderer = FullRenderer[cmbRedCar4K2.SelectedIndex];
                                myTemplate = FullTemplate[cmbRedCar4K2.SelectedIndex];
                            }
                        }
                        if (encode == 3)
                        {
                            if (file.Contains("SampleProj"))
                            {
                                if (!cbSampProj4K3.Checked) { continue; }
                                myRenderer = FullRenderer[cmbSampProj4K3.SelectedIndex];
                                myTemplate = FullTemplate[cmbSampProj4K3.SelectedIndex];
                            }
                            else
                            {
                                if (!cbRedCar4K3.Checked) { continue; }
                                myRenderer = FullRenderer[cmbRedCar4K3.SelectedIndex];
                                myTemplate = FullTemplate[cmbRedCar4K3.SelectedIndex];
                            }
                        }
                    }

                    if (!dryrun)
                    {
                        myVegas.NewProject(false, false);
                        myVegas.OpenProject(file);
                    }

                    bestOfSeconds = int.MaxValue;
                    bestOfTimes = TimeSpan.MaxValue;

                    // do each render multiple times but track best of times for csv
                    int[] reps = { 1, 2 };
                    foreach (int rep in reps)
                    {
                        if (dryrun) 
                        { 
                            fileCount++; 
                        } 
                        else 
                        {
                            //if (exit) { running = false; return; }
                            if (exit) { return; }
                            foo = "Renders to do: " + fileCount.ToString() + 
                                "\nRenderer: " + myRenderer + 
                                "\nTemplate: " + myTemplate +
                                "\nProject: " + file;
                            this.mymsg.Visible = true;
                            this.mymsg.Text = foo;
                            this.mymsg.Refresh();

                            //int thisTimeSeconds = 0;
                            int timeTakenSeconds = 0;
                            thisTimeTaken = TimeSpan.Zero;
                            var timer = new Stopwatch();

                            RStart = new Timecode("00:00:00:00");
                            RLength = myVegas.Project.Length;

                            //   ********* RENDER 1 PROJECT BEGIN *********
                            //   ******************************************
                            //   ******************************************
                            running = true;
                            timer.Start();
                            DoRender(FullFileName, myRenderer, myTemplate, RStart, RLength, false, false);
                            timer.Stop();
                            running = false;
                            //   ********* ENDED *********

                            if (exit) { return; }

                            //TimeSpan timeTaken = timer.Elapsed;
                            thisTimeTaken = timer.Elapsed;
                            foo = "Renders to do: " + fileCount.ToString() +
                                "\nRenderer: " + myRenderer +
                                "\nTemplate: " + myTemplate +
                                "\nProject: " + file +
                                "\nElapsed Time: " + thisTimeTaken.ToString(@"m\:ss\.fff") + " ("+ thisTimeTaken.Seconds.ToString() + " seconds)";
                            this.mymsg.Text = foo;
                            this.mymsg.Refresh();

                            timeTaken = thisTimeTaken;
                            timeTakenSeconds = thisTimeTaken.Seconds;
 
                            //Adjust TimeTaken to equal best of times
                            if (timeTakenSeconds < bestOfSeconds)
                            {
                                bestOfSeconds = timeTakenSeconds;
                                bestOfTimes = timeTaken;
                            }
                            timeTaken = bestOfTimes;
                            timeTakenSeconds = bestOfSeconds;

                            if (myRenderer.Name.Contains("MAGIX AVC")) { csvRender = "Magix Avc"; }
                            else { csvRender = myRenderer.Name.Substring(0, myRenderer.Name.IndexOf(" ", 7)); }

                            if (myTemplate.Name.Contains("NVenc")) { csvEncode = "NVenc"; }
                            else if (myTemplate.Name.Contains("QSV)")) { csvEncode = "QSV"; }
                            else if (myTemplate.Name.Contains("MC")) { csvEncode = "MC"; }
                            else if (myTemplate.Name.Contains("VCE")) { csvEncode = "VCE"; }
                            else { csvEncode = myTemplate.ToString(); }

                            if (myTemplate.Name.Contains("1080p")) { csvFrame = "FHD"; }
                            else if (myTemplate.Name.Contains("2160p")) { csvFrame = "UHD"; }
                            else { csvFrame = myTemplate.ToString(); }

                            if (FullFileName.Contains("10 Sample")) { csvSampProj = timeTaken.ToString(@"m\:ss"); }
                            else if (FullFileName.Contains("20 RedCar")) { csvRedCar = timeTaken.ToString(@"m\:ss"); }
                            else if (FullFileName.Contains("30 4k RedCar")) { csvRedCar4K = timeTaken.ToString(@"m\:ss"); }
                            else if (FullFileName.Contains("40 RedCar HEVC")) { csvRedCarHevc = timeTaken.ToString(@"m\:ss"); }
                            else if (FullFileName.Contains("50 RedCar ProRes")) { csvRedCarProRes = timeTaken.ToString(@"m\:ss"); }
  
                            fileCount--;

                            if (fileCount > 0 && !exit )
                            {
                                foo = "GPU cooldown delay... Renders to do: " + fileCount.ToString() +
                                    "\nRenderer: " + myRenderer +
                                    "\nTemplate: " + myTemplate +
                                    "\nProject: " + file +
                                    "\nElapsed Time: " + thisTimeTaken.ToString(@"m\:ss\.fff");
                                this.mymsg.Text = foo;
                                this.mymsg.Refresh();
                                progressBar1.Minimum = 0;
                                progressBar1.Maximum = sleepCount;
                                progressBar1.Value = 0;
                                progressBar1.Step = 1;
                                progressBar1.Refresh();
                                progressBar1.Visible = true;
                                //                            
                                for (int i = 0; i < sleepCount; i++)
                                {
                                    // programmed delay
                                    wait(20);

                                    if (exit) break;
                                    progressBar1.PerformStep();
                                    progressBar1.Refresh();
                                }
                                progressBar1.Visible = false;

                                foo = "Renders to do: " + fileCount.ToString() +
                                    "\nRenderer: " + myRenderer +
                                    "\nTemplate: " + myTemplate +
                                    "\nProject: " + file +
                                    "\nElapsed Time: " + thisTimeTaken.ToString(@"m\:ss\.fff");
                                this.mymsg.Text = foo;
                                this.mymsg.Refresh();
                                mymsg.Visible = true;
                            }
                            if (fileCount == 0 && !exit)
                            {
                                myVegas.NewProject(false, false);
                                exit = true;
                                wait(6000);
                                mymsg.Text = "All Done.";
                                btnRender.Text = "Render";
                                btnRender.Refresh();
                                mymsg.Refresh();
                                System.Threading.Thread.Sleep(3000);
                            } 
                            
                            if (fileCount > 0 && exit) 
                            { 
                                // gets here if our Cancel button was used... needed for Vegas versions before v18
                                mymsg.Text = "User Cancelled.";
                                exit = true;
                                mymsg.Refresh();
                                //System.Threading.Thread.Sleep(3000);
                                wait(3000);
                            }
                        }

                    } // end reps

                    // ouput best of times for the reps just completed
                    //
                    if (!dryrun)
                    {
                        // now that we know the Frame, Render, Encoder, and Decoder... output them to the csv... but only once
                        if (!framestuff)
                        {
                            File.AppendAllText(txtDirBox.Text + csvFile,
                             "," + csvFrame + "," +
                             csvRender + "," +
                             csvEncode + "," +
                             csvDecoder);
                            framestuff = true;
                        }

                        //if (csvSampProj != "" && csvRedCar == "" && csvRedCar4K == "" && csvRedCarHevc == "" && csvRedCarProRes == "")
                        //                      //if (FullFileName.Contains("10 Sample")) { csvSampProj = timeTaken.ToString(@"m\:ss"); }
                        //                      //else if (FullFileName.Contains("20 RedCar")) { csvRedCar = timeTaken.ToString(@"m\:ss"); }
                        //                      //else if (FullFileName.Contains("30 4k RedCar")) { csvRedCar4K = timeTaken.ToString(@"m\:ss"); }
                        //                      //else if (FullFileName.Contains("40 RedCar HEVC")) { csvRedCarHevc = timeTaken.ToString(@"m\:ss"); }
                        //                      //else if (FullFileName.Contains("50 RedCar ProRes")) { csvRedCarProRes = timeTaken.ToString(@"m\:ss"); }

                        if (FullFileName.Contains("10 Sample"))
                        {
                            File.AppendAllText(txtDirBox.Text + csvFile,
                                "," + csvSampProj);
                        }

                        else if (FullFileName.Contains("20 RedCar"))
                        {
                            if (csvSampProj == "")
                            {
                                //files before were skipped so output all 
                                File.AppendAllText(txtDirBox.Text + csvFile,
                                    "," + csvSampProj + "," + csvRedCar);
                            }
                            else
                            {
                                // none before skipped so just output this one
                                File.AppendAllText(txtDirBox.Text + csvFile,
                                    "," + csvRedCar);
                            }
                        }

                        else if (FullFileName.Contains("30 4k RedCar"))
                        {
                            if (csvSampProj == "" && csvRedCar == "")
                            {
                                //files before were skipped so output all 
                                File.AppendAllText(txtDirBox.Text + csvFile,
                                    "," + csvSampProj + "," + csvRedCar + "," + csvRedCar4K);
                            }
                            else if (csvRedCar == "")
                            {
                                //Only the file before was skipped so output the 2 of them
                                File.AppendAllText(txtDirBox.Text + csvFile,
                                    "," + csvRedCar + "," + csvRedCar4K);
                            }
                            else
                            {
                                // none before skipped so just output this one
                                File.AppendAllText(txtDirBox.Text + csvFile,
                                    "," + csvRedCar4K);
                            }
                        }

                        else if (FullFileName.Contains("40 RedCar"))
                        {

                            if (csvSampProj == "" && csvRedCar == "" && csvRedCar4K == "")
                            {
                                //all files before were skipped so output all 
                                File.AppendAllText(txtDirBox.Text + csvFile,
                                    "," + csvSampProj + "," + csvRedCar + "," + csvRedCar4K + "," + csvRedCarHevc);
                            }
                            else if (csvRedCar == "" && csvRedCar4K == "")
                            {
                                //only two files before were skipped so output the three
                                File.AppendAllText(txtDirBox.Text + csvFile,
                                    "," + csvRedCar + "," + csvRedCar4K + "," + csvRedCarHevc);
                            }
                            else if (csvRedCar4K == "")
                            {
                                //Only the file before was skipped so output the 2 of them
                                File.AppendAllText(txtDirBox.Text + csvFile,
                                    "," + csvRedCar4K + "," + csvRedCarHevc);
                            }
                            else
                            {
                                // none before skipped so just output this one
                                File.AppendAllText(txtDirBox.Text + csvFile,
                                    "," + csvRedCarHevc);
                            }
                        }

                        else if (FullFileName.Contains("50 RedCar"))
                        {
                            if ((csvSampProj == "" && csvRedCar == "" && csvRedCar4K == "") && csvRedCarHevc == "")
                            {
                                // all files before were skipped so output all
                                File.AppendAllText(txtDirBox.Text + csvFile,
                                    "," + csvSampProj + "," + csvRedCar + "," + csvRedCar4K + "," + csvRedCarHevc + "," + csvRedCarProRes);
                            }
                            else if (csvSampProj == "" && csvRedCar == "" && csvRedCar4K == "")
                            {
                                //three files before were skipped so output all four
                                File.AppendAllText(txtDirBox.Text + csvFile,
                                    "," + csvRedCar + "," + csvRedCar4K + "," + csvRedCarHevc + "," + csvRedCarProRes);
                            }
                            else if (csvRedCar == "" && csvRedCar4K == "")
                            {
                                //only two files before were skipped so output the three
                                File.AppendAllText(txtDirBox.Text + csvFile,
                                    "," + csvRedCar4K + "," + csvRedCarHevc + csvRedCarProRes);
                            }
                            else if (csvRedCar4K == "")
                            {
                                //Only the file before was skipped so output the 2 of them
                                File.AppendAllText(txtDirBox.Text + csvFile,
                                    "," + csvRedCarHevc + "," + csvRedCarProRes);
                            }
                            else
                            {
                                // none before skipped so just output this one
                                File.AppendAllText(txtDirBox.Text + csvFile,
                                    "," + csvRedCarProRes);
                            }
                        }

                    }

                } // end files

                if (!dryrun)
                {
                    /*
                    // update csv file after reps for each file completed...
                    // expect that render time has been updated after each rep
                    // (default is time for last rep unless adjusted in render function)
                    //                    string date = DateTime.UtcNow.ToString("yyyy-MM-dd");

                    // File.AppendAllText(txtDirBox.Text + "render_times.csv",
                    File.AppendAllText(txtDirBox.Text + csvFile,
                        csvVersion + "," +
                        csvCpu + "," +
                        csvCores + "," +
                        csvGpu + "," +
                        csvIgpu + "," +
                        csvFrame + "," +
                        csvRender + "," +
                        csvEncode + "," +
                        csvDecoder + "," +
                        csvSampProj + "," +
                        csvRedCar + "," +
                        csvRedCar4K + "," +
                        csvRedCarHevc + "," +
                        csvRedCarProRes + "\n");
                    */
                    // since all timings already output, just pump out a linefeed
                    //File.AppendAllText(txtDirBox.Text + csvFile, "\n");
                }

            } // end encodes

            if (dryrun) return;

            myVegas.NewProject(false, false);
        } // end renderProjects()



        public void DoRender(string fileName, Renderer rndr, RenderTemplate rndrTemplate, Timecode start, Timecode length, bool IncMarkers, bool SetStretch)
        {
            // make sure the file does not already exist
            bool overwriteExistingFiles = true;

            if (!overwriteExistingFiles && File.Exists(fileName))
            {
                MessageBox.Show("File already exists: " + fileName);
                return;
            }

            //MessageBox.Show(fileName);

            if (File.Exists(fileName))
            {
                try
                {
                    File.Delete(fileName);
                }
                catch
                {
                }
            }

            if (File.Exists(fileName))
            {
                try
                {
                    myVegas.Project.MediaPool.Remove(fileName);
                    myVegas.UpdateUI();
                    File.Delete(fileName);
                }
                catch
                {
                }
            }

            if (File.Exists(fileName))
            {
                MessageBox.Show("Could not delete: " + fileName);
                return;
            }


            // perform the render. The Render method returns
            // a member of the RenderStatus enumeration. If
            // it is anything other than OK, exit the loops.
            RenderArgs args = new RenderArgs();
            args.OutputFile = fileName;
            args.RenderTemplate = rndrTemplate;
            args.Start = start;
            args.Length = length;
            args.IncludeMarkers = IncMarkers;
            args.StretchToFill = SetStretch;


            // ********* VEGAS RENDER ****************
            // ********* VEGAS RENDER ****************

            RenderStatus status = myVegas.Render(args);

            if (exit) {
                btnRender.Text = "Render";
                btnRender.Enabled = true;
                return; 
            }

            //RenderStatus status = Vegas.Render(fileName, rndrTemplate, start, length);

            // if the render completed successfully, just return
            if (status == RenderStatus.Complete)
                return;

            // if the user canceled, throw out a special message that won't be
            // displayed.

            if (status == RenderStatus.Canceled)
            {
                MessageBox.Show("User canceled");
                btnRender.Text = "Render";
                mymsg.Text = "User cancelled.";
                mymsg.Refresh();
                btnRender.Refresh();
                exit = true;
                //running = false;
                return;
            }
 
            // if the render failed, throw out a detailed error message.
            StringBuilder msg = new StringBuilder("Render failed:\n");
            msg.Append("\n file name: ");
            msg.Append(fileName);
            msg.Append("\n Renderer: ");
            msg.Append(rndr.FileTypeName);
            msg.Append("\n Template: ");
            msg.Append(rndrTemplate.Name);
            msg.Append("\n Start Time: ");
            msg.Append(start.ToString());
            msg.Append("\n Length: ");
            msg.Append(length.ToString());
            MessageBox.Show(Convert.ToString(msg));
            this.BringToFront();
        }

        private void btnProjects_Click(object sender, EventArgs e)
        {
            string ofd_dir;

            hideCfg();
            filelist = "";

            OpenFileDialog ofd = new OpenFileDialog();

            ofd.Filter = "VEGAS Projects (*.VEG)|*.VEG|All files (*.*)|*.*";
            ofd.Multiselect = true;
            ofd.Title = "Select the VEGAS Projects to render";
            ofd.InitialDirectory = this.txtProjDir.Text;
            ofd_dir = ofd.CustomPlaces.ToString();
            DialogResult dr = ofd.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                //                this.txtProjDir.Text = DialogResult.ToString() ;

                fileNames = ofd.FileNames;
                GotFileNames = true;
                txtProjDir.Text = Path.GetDirectoryName(fileNames[0]);
                txtProjDir.Refresh();

                foreach (String file in fileNames)
                {
                    filelist = filelist + file + "\n";
                }
                //MessageBox.Show("Projects to be rendered: \n" + filelist);
                this.mymsg.Text = "Projects to be rendered: \n" + filelist;
                this.mymsg.Refresh();
                this.mymsg.Visible = true;
            }
            else
            {
                GotFileNames = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            hideCfg();
            MessageBox.Show("" + "AutoBench\n"+
                "build: " + build + "\n" +
                "Designed by: Howard Vigorita\n" +
                "derived from JETDV tutorials... \n" +
                "see: http://www.jetdv.com/");
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            txtDirBox.Text = GetSaveDir(txtDirBox.Text);
            hideCfg();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void tslConfig_Click(object sender, EventArgs e)
        {
            SleepAdjust.Visible = true;
            SleepAmt.Visible = true;
            cbHideCfg.Visible = true;
        }

        private void SleepAmt_Scroll(object sender, EventArgs e)
        {
            sleepCount = SleepAmt.Value * 50;
        }

        private void cbHideCfg_CheckedChanged(object sender, EventArgs e)
        {
            hideCfg();
        }

        private void SleepAdjust_Click(object sender, EventArgs e)
        {
            hideCfg();
        }

        private void CheckAll_CheckedChanged(object sender, EventArgs e)
        {
            if (CheckAll.Checked==false)
            {
                cbRedCarHD1.Checked = false;
                cbRedCarHD2.Checked = false;
                cbRedCarHD3.Checked = false;
                cbRedCar4K1.Checked = false;
                cbRedCar4K2.Checked = false; 
                cbRedCar4K3.Checked = false;
                cbSampProjHD1.Checked = false;
                cbSampProjHD2.Checked = false;
                cbSampProjHD3.Checked = false;
                cbSampProj4K1.Checked = false;
                cbSampProj4K2.Checked = false;
                cbSampProj4K3.Checked = false;
            }
            else 
            {
                cbRedCarHD1.Checked = true;
                cbRedCarHD2.Checked = true;
                cbRedCarHD3.Checked = true;
                cbRedCar4K1.Checked = true;
                cbRedCar4K2.Checked = true;
                cbRedCar4K3.Checked = true;
                cbSampProjHD1.Checked = true;
                cbSampProjHD2.Checked = true;
                cbSampProjHD3.Checked = true;
                cbSampProj4K1.Checked = true;
                cbSampProj4K2.Checked = true;
                cbSampProj4K3.Checked = true;
            }
        }

        private void txtCpu_TextChanged(object sender, EventArgs e)
        {
            myCpu = txtCpu.Text;
        }
    }
}

public class EntryPoint
{
        private static hv_bench.Form1 form;

        public void FromVegas(Vegas vegas)
        {


        form = new hv_bench.Form1(vegas);


            form.ShowDialog();

    }
}

