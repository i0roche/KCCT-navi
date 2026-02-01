using System;

namespace team1 //stripmenuのイベントハンドラたち
{
    public partial class Form1
    {
        private void OnGeneralBuildingMenuClick(object sender, EventArgs e) { }
        private void OnGeneralBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("一般科", 1);
        private void OnGeneralBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("一般科", 2);
        private void OnGeneralBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("一般科", 3);
        private void OnGeneralBuildingFloor4Click(object sender, EventArgs e) => MoveToBuilding("一般科", 4);
        private void OnGeneralBuildingFloor5Click(object sender, EventArgs e) => MoveToBuilding("一般科", 5);
        private void OnGeneralBuildingFloor6Click(object sender, EventArgs e) => MoveToBuilding("一般科", 6);

        private void OnSpecializedBuildingMenuClick(object sender, EventArgs e) { }
        private void OnElecBuildingMenuClick(object sender, EventArgs e) { }
        private void OnElecBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 1);
        private void OnElecBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 2);
        private void OnElecBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 3);
        private void OnElecBuildingFloor4Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 4);

        private void OnDenshiBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 1);
        private void OnDenshiBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 2);
        private void OnDenshiBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 3);
        private void OnDenshiBuildingFloor4Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 4);

        private void OnToshiBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 1);
        private void OnToshiBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 2);
        private void OnToshiBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 3);
        private void OnToshiBuildingFloor4Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 4);

        private void OnMechBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 1);
        private void OnMechBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 2);
        private void OnMechBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 3);
        private void OnMechBuildingFloor4Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 4);

        private void OnChemBuildingMenuClick(object sender, EventArgs e) { }
        private void OnChemBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 1);
        private void OnChemBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 2);
        private void OnChemBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 3);
        private void OnChemBuildingFloor4Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 4);

        private void OnAdvancedBuildingMenuClick(object sender, EventArgs e) { }
        private void OnAdvancedBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("専攻科棟", 1);
        private void OnAdvancedBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("専攻科棟", 2);
        private void OnAdvancedBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("専攻科棟", 3);

        private void OnHonbuBuildingMenuClick(object sender, EventArgs e) { }
        private void OnHonbuBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("本部棟", 1);
        private void OnHonbuBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("本部棟", 2);

        private void OnKougiBuildingMenuClick(object sender, EventArgs e) { }
        private void OnKougiBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 1);
        private void OnKougiBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 2);
        private void OnKougiBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 3);
        private void OnKougiBuildingFloor4Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 4);

        private void OnGakuseiBuildingMenuClick(object sender, EventArgs e) { }
        private void OnGakuseiBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("学生会館", 1);
        private void OnGakuseiBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("学生会館", 2);
        private void OnGakuseiBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("学生会館", 3);

        private void OnLibraryBuildingMenuClick(object sender, EventArgs e) { }
        private void OnLibraryBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("図書館", 1);
        private void OnLibraryBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("図書館", 2);
    }
}
