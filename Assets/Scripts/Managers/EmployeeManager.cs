using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmployeeManager : MonoBehaviour
{
    public List<Employee> employees = new List<Employee>();
    public List<GameObject> employeeButtons = new List<GameObject>();

    public DepartmentManager deptManager;

    public Sprite[] employeeSprites;

    public GameObject employeeGrid;
    public GameObject employeeBtnPrefab;

    public GameObject elevatorMarker;

    public EmployeeInspectWidget employeeInspectWidget;

    public Animator elevator;

    public StudioEventEmitter fireEmployee;
    public StudioEventEmitter payEmployeeSound;
    public StudioEventEmitter makeMoneySound;

    public DeskButton fireButton;
    public DeskButton dismissButton;
    public DeskButton startProjectButton;
    public SpriteRenderer employeeOfficeSprite;


    public float totalProd;
    public int totalProjectsCompleted = 0;
    public float prodGoal = 100;
    private float startingProdGoal;
    public float prodReward = 1;
    private float startingProdReward;
    public int completedProjectsBeforeRewardIncrease;
    public float totalCost;
    public float avgLikeability;
    public float avgMorale;
    public float totalValue;
    public float moraleBoostPership;
    public int numStartEmployees = 1;

    public Slider prodSlider;
    public Slider costSlider;
    public Slider likeabilitySlider;
    public Text valueText;
    public Text employeeLedgerText;
    public bool fire;
    public bool hire;
    public bool isWorkingOnProject;

    public int selectedEmployee;

    public float fireAnimDelay;
    public float fireRealDelay;

    public GameObject inTheRedAlarm;
    public GameObject computerScreen;

    private void Start()
    {
        PopulateEmployeeList();
        PrintEmployees();

        fireButton.onButtonPress.AddListener(FireEmployee);
        dismissButton.onButtonPress.AddListener(DismissEmployee);
        startProjectButton.onButtonPress.AddListener(StartWorkingOnProject);
        startingProdGoal = prodGoal;
        startingProdReward = prodReward;

        FMOD.Studio.EventInstance musicEvent = GameObject.Find("Music").GetComponent<StudioEventEmitter>().EventInstance;
        musicEvent.setPaused(true);
    }

    private void Update()
    {
        if(totalValue < 0 && !inTheRedAlarm.activeInHierarchy)
        {
            inTheRedAlarm.SetActive(true);
            inTheRedAlarm.GetComponent<StudioEventEmitter>().Play();
        }
        else if(totalValue > 0 && inTheRedAlarm.activeInHierarchy)
        {
            inTheRedAlarm.SetActive(false);
        }

        if (fire)
        {
            if (selectedEmployee < employees.Count)
            {
                RemoveEmployee(employees[selectedEmployee]);
                CameraShake.Instance.StartShake(.5f, 0.05f);
            }
            fire = false;

        }
        if (hire)
        {
            Hire();
            hire = false;
            

        }
        if (Input.GetKeyDown(KeyCode.R)) 
        {
            ResetEmployeeList();
            PrintEmployees();
        }
    }

    public void UpdateValues()
    {
        if(totalProd >= prodGoal)
        {
            ShipProduct();
            totalProd = 0;
            isWorkingOnProject = false;

        }

        totalCost = CalculateCost();
        avgLikeability = CalculateAvgLikeability();
        avgMorale = CalculateAvgMorale();

        prodSlider.value = totalProd / prodGoal;
        costSlider.value = totalCost / 5;
        //likeabilitySlider.value = avgLikeability / 5;

        prodSlider.GetComponentInChildren<Text>().text = "Progress To Ship: " + totalProd.ToString("F");
        costSlider.GetComponentInChildren<Text>().text = "Payroll Cost: " + totalCost.ToString("F");
        //likeabilitySlider.GetComponentInChildren<Text>().text = "Avg. Likeability: " + avgLikeability.ToString("F") + "/5";

        valueText.text = "Company Value: " + totalValue.ToString("F");
        //employeeLedgerText.text = EmployeeLedgerReadout();
    }
    private string EmployeeLedgerReadout()
    {
        string readout = "";
        int i=0;
        foreach (Employee e in employees)
        {
            readout += "#";
            readout += i.ToString() + " ";
            i++;
           
            readout += " || Prod: " + e.productivity.ToString("F1");
            readout += " || Sal: " + e.salary.ToString("F1");
            readout += " || Morale: " + e.morale.ToString("F1");
            readout += " || Likeability: " + e.likeability.ToString("F1");
            readout += " || Ships: " + e.successfulShips.ToString("F1");
            readout += " || Age: " + e.yrsAtJob.ToString("F1");
            readout += " || Name: " + e.name;
            readout += "\n";
        }
            return readout;
    }


    private void ResetEmployeeList()
    {
        employees.Clear();
        PopulateEmployeeList();
    }

    private void PopulateEmployeeList()
    {
        for (int i = 0; i < numStartEmployees; i++)
        {
            AddEmployee(new Employee(), i);
        }
    }

    public void AddEmployee(Employee e, int index)
    {
        employees.Add(e);
        GameObject empBtn = Instantiate(employeeBtnPrefab, employeeGrid.transform);
        EmployeeButton empBtnComp = empBtn.GetComponent<EmployeeButton>();
        employeeButtons.Add(empBtn);
        //empBtnComp.SetSprite(e.employeeSprite);
        empBtnComp.employee = e;
        empBtnComp.manager = this;
        empBtnComp.employeeIndex = index;
        empBtnComp.GetComponentInChildren<Text>().text = index.ToString();

    }

    public void RemoveEmployee(Employee e)
    {
        foreach(GameObject empBtn in employeeButtons)
        {
            if(empBtn.TryGetComponent<EmployeeButton>(out EmployeeButton empBtnComp))
            {
                if(empBtnComp.employee == e)
                {
                    employees.Remove(e);
                    employeeButtons.Remove(empBtn);
                    Destroy(empBtn);
                    return;
                }
            }
        }
        
    }

    public void DismissEmployee()
    {
        if (selectedEmployee != -1)
        {
            Debug.Log("Dismissed");
            employeeButtons[selectedEmployee].GetComponent<EmployeeButton>().DismissEmployeeFromOffice();
        }
        else
        {
            Debug.Log("No selected employee");
        }
        
    }

    public void FireEmployee()
    {
        if(selectedEmployee != -1)
        {
            Debug.Log("FIRED!");
            employeeInspectWidget.visContainer.SetActive(false);
            RemoveEmployee(employees[selectedEmployee]);
            selectedEmployee = -1;
            GameObject.Find("Doors").GetComponent<Animator>().SetTrigger("close");
            StartCoroutine(FireEmployeeFromCannon());
        }
        else
        {
            Debug.Log("No selected employee");
        }
    }

    public void SetEmployeeOfficeSprite(Sprite empSprite)
    {
        employeeOfficeSprite.sprite = empSprite;
    }

    public void Hire()
    {
        AddEmployee(new Employee(), employees.Count);
    }

    private void PrintEmployees()
    {
        foreach (Employee e in employees)
        {
            Debug.Log(e.StatsString());
        }
    }

    public void AddProgressToShip()
    {
        totalProd += CalculateProductivity();
        
    }
    public void StartWorkingOnProject()
    {
        if (!isWorkingOnProject)
        {
            BackgroundScrollManager.instance.baseSpeed = 0.5f;
            deptManager.tankMoving = true;

            FMOD.Studio.EventInstance musicEvent = GameObject.Find("Music").GetComponent<StudioEventEmitter>().EventInstance;
            musicEvent.setPaused(false);

            foreach (EnemyBuilding eb in FindObjectsOfType<EnemyBuilding>())
            {
                eb.speed = 0.01f;
            }


            isWorkingOnProject = true;
            float projectIncreaseFactor = Mathf.Floor(totalProjectsCompleted / completedProjectsBeforeRewardIncrease);
            print("project increase factor: " + projectIncreaseFactor);
            prodGoal =  startingProdGoal + projectIncreaseFactor * startingProdGoal;
            prodReward = startingProdReward + projectIncreaseFactor * startingProdReward;
        }
    }
    public void WorkEmployees()
    {
        if (isWorkingOnProject)
        {
            AddProgressToShip();
            foreach (Employee e in employees)
            {
                e.morale -= e.moraleLossPerLeech;
                if (e.morale < 0)
                {
                    e.morale = 0;

                }
                print("morale: " + e.morale);
            }
        }
        

    }

    private IEnumerator FireEmployeeFromCannon()
    {
        yield return new WaitForSeconds(fireAnimDelay);
        fireEmployee.Play();
        deptManager.companyHead.GetComponent<Animator>().SetTrigger("shoot");

        //real world sound
        yield return new WaitForSeconds(fireRealDelay);
        CameraShake.Instance.StartShake(1.6f, 0.2f);
    }

    public void PayEmployeeSalaries()
    {
        if(totalValue > 0)
        {
            foreach (Employee e in employees)
            {
                payEmployeeSound.Play();
                totalValue -= e.salary;
            }
        }
        else
        {
            StartCoroutine(Lose());
        }
    }

    public float CalculateProductivity()
    {
        float prod = 0;
        foreach(Employee e in employees)
        {
            prod += e.productivity * avgLikeability * Mathf.Max(e.morale, 1f)  ;
        }
        return prod;
    }
    private void ShipProduct()
    {
        BackgroundScrollManager.instance.baseSpeed = 0f;
        deptManager.tankMoving = false;
        FMOD.Studio.EventInstance musicEvent = GameObject.Find("Music").GetComponent<StudioEventEmitter>().EventInstance;
        musicEvent.setPaused(true);

        foreach(EnemyBuilding eb in FindObjectsOfType<EnemyBuilding>())
        {
            if(!eb.destroyed)
            {
                eb.speed = 0f;
            }
        }

        makeMoneySound.Play();


        totalValue += prodReward;
        totalProjectsCompleted++;
        foreach (Employee e in employees)
        {
            e.successfulShips++;
            //every time you ship a product you feel good
            e.morale += e.moraleGainPerShip;
            if (e.morale > 5)
            {
                e.morale = 5;
            }
            e.morale+=e.moraleGainPerShip;
            //every time you ship a product, you feel a little less good about it, until you stop caring
            e.moraleGainPerShip -= (e.maxPassion - e.passion) * 0.2f;
            if (e.moraleGainPerShip < 0) {
                e.moraleGainPerShip = 0;
            }
            
        }
    }

    public float CalculateCost() 
    {
        float cost = 0;
        foreach (Employee e in employees)
        {
            cost += e.salary;
        }
        return cost;
    }

    private float CalculateAvgLikeability()
    {
        float totalLikeability = 0;
        foreach(Employee e in employees)
        {
            totalLikeability += e.likeability;
        }

        return (totalLikeability / employees.Count);
    }
    public float CalculateAvgMorale()
    {

        if (employees.Count > 0)
        {
            float totalMorale = 0;
            foreach (Employee e in employees)
            {
                totalMorale += e.morale;
            }

            return (totalMorale / employees.Count);
        }
        else
        {
            return 0;
        }
    }

    private IEnumerator Lose()
    {
        deptManager.companyHead.GetComponent<Animator>().SetTrigger("dead");
        fireButton.SetButtonActive(false);
        dismissButton.SetButtonActive(false);
        startProjectButton.SetButtonActive(false);

        yield return new WaitForSeconds(5);
        computerScreen.SetActive(false);
    }
}
