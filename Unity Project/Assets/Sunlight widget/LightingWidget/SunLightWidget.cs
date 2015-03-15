//solar shading calculator
//written by Yifan Liu - ivenaieseccqu@gmail.com
//April. 26th, 2013
// based on fomula from http://www.esrl.noaa.gov/gmd/grad/solcalc/calcdetails.html

// this script calculates the zimuth and altitude angle, which matches(+-1 difference): http://pveducation.org/pvcdrom/properties-of-sunlight/sun-position-calculator
// also matches (+-2): http://www.esrl.noaa.gov/gmd/grad/solcalc/
// the difference is caused by compiler lose of decimal precision when calculating julianday. The compiler loses precision when handling floats with more than seven digits

//INSTRUCTION: Positive z axis is the default NORTH, positive x axis is the default EAST
//euler.y of sun = Azmuith angle of sun -180;
//euler.x of sun = Altitude angle of sun;
//!!!Make sure align the north of the site to the Z axis before using this script.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class SunLightWidget : MonoBehaviour {
	public static SunLightWidget Instance;

	public SunlightWidgetData InputData; // The center database of all the input data needed by the calculation algorithm
	public string xmlPath;

	public RectTransform cityButPrefab;
	public RectTransform cityDropdown;
	public Text yearLabelText;
	public Text monthLabelText;
	public Text dateLabelText;

	public Transform sun;
	public static DateTime time = new DateTime(); // see the DateTime script for class definition
	public float longitude;
	public float latitude;
	public float localTime;	
	public float timeZone;
	
	public float altitude;
	public float azimuth;
	
	public float julianDay;
	public float julianCentury;
	public float geomMeanLongSun;
	public float geomMeanAnomSun;
	public float eccentEarthOrbit;
	public float sunEqOfCtr;
	public float sunTrueLong;
	public float sunAppLong;
	public float meanObliqEcliptic;
	public float ObliqCorr;
	public float declinationAngle;
	public float varY;
	public float eqOfTime;
	public float trueSolarTime;
	public float hourAngle;
	public float zenithAngle;
	
	public string yearString;
	public string monthString;
	public string dayString;
	public string minutesString;
	public string longitudeString;
	public string latitudeString;
	public string timeZoneString;
	
	public Transform yearInput;
	public Transform monthInput;
	public Transform dateInput;
	public Transform timeZoneInput;
	public Transform LongitudeInput;
	public Transform LatitudeInput;

	public Transform hourSlider;
	public Transform selectCityList;
	public UIPopupList selectDateList;
	public UICheckbox checkboxScript;
	public Transform dayLightCheckbox;
	public UILabel startButtonLabel;
	public UILabel hourNumberLabel;
	public UIPopupList cityList; // the list to select city
	public GameObject panel; // the panle object. At the start of the script, we will fill the transforms of the 
								//scripts with the children of this object
	
	private bool widgetStart; // default is false, turn to true when the "start widget" is clicked
	public bool dayLightSaving; // default is false, true -> daylight saving -> deduct an hour from localtime during calculation
	
	protected UILabel yearStringUIScript;
	protected UISprite startButtonSprite;

	void Awake(){
		xmlPath = Application.dataPath + "/sunlightWidgetData.xml";
		if(Instance == null){
			Instance = this;
		}
		else{
			Debug.LogError("Error: two instances of sunlightwidget");
		}
	}

	// Use this for initialization
	//PRE: Assign panel to the panel variable in inspector
	void Start () {
		if(true){//need to check for file path validity
		InputData  = XmlIO.Load(xmlPath, typeof(SunlightWidgetData)) as SunlightWidgetData;
		}
		Debug.Log("input loaded: " + InputData);

		//this section initializes the UI with the inputData loaded from the xml
		populateCityDropDown(InputData.ListOfCity, cityDropdown,cityButPrefab);
		populateTimeLabels(InputData);
//		SunlightWidgetData data = new SunlightWidgetData();

//		XmlIO.Save(data ,Application.dataPath + "/sunlightWidgetData.xml");

		yearString = "2013";
	 	monthString = "4";
	 	dayString = "23";
	 	minutesString = "0.004166667";
		longitudeString = "-75";
		latitudeString = "38.5";
		timeZoneString = "-4";
		widgetStart = false;
		sun = this.transform.Find("Sun").transform;
		panel = this.transform.parent.FindChild("Camera/Anchor/Panel").gameObject;
		
		yearInput = panel.transform.Find("YearInput");
		monthInput = panel.transform.Find("MonthInput");
		dateInput = panel.transform.Find("DateInput");
		timeZoneInput =  panel.transform.Find("TimeZoneInput");
		LongitudeInput = panel.transform.Find("LongitudeInput");
		LatitudeInput = panel.transform.Find("LatitudeInput");
		hourSlider = panel.transform.Find("HourSlider");
		selectCityList = panel.transform.Find("SelectCityList");
		cityList = selectCityList.GetComponent<UIPopupList>();
		dayLightCheckbox = panel.transform.Find("DaylightSaving");
		startButtonLabel = panel.transform.Find("StartButton/Label").GetComponent<UILabel>();
		startButtonSprite = panel.transform.Find("StartButton/Background").GetComponent<UISprite>();
		hourNumberLabel = panel.transform.Find("HourNumber").GetComponent<UILabel>();
		selectDateList = panel.transform.Find("SelectDateList").GetComponent<UIPopupList>();
		checkboxScript = dayLightCheckbox.GetComponent<UICheckbox>();
		
		dayLightSaving = true; // It must be set to true at the beginning as at the start of the game, the daylight saving box is checked
								// and will be unchecked automatically
	}
	
	void populateCityDropDown(List<City> listOfCity, RectTransform dropdownPanel, RectTransform cityButPrefab){
		//remove existing item in the dropdown
		foreach(RectTransform item in dropdownPanel){
			Destroy (item.gameObject);
		}

		foreach(City city in listOfCity){
			RectTransform cityItem = Instantiate(cityButPrefab) as RectTransform;
			cityItem.parent = dropdownPanel;
			cityItem.FindChild("Text").GetComponent<UnityEngine.UI.Text>().text = city.CityName;
		}
	}

	void populateTimeLabels(SunlightWidgetData database){
		dateLabelText.text = database.Date.ToString();
		monthLabelText.text = database.Month.ToString();
		yearLabelText.text = database.Year.ToString();
	}

	//save the inputDate to the xml file at path Application.dataPath + "/sunlightWidgetData.xml"
	public void saveDataToXML(){
		XmlIO.Save(InputData, xmlPath);
	}

	//get called when changing hourSlider
	public void OnDrag(){
		if(widgetStart){
			getInputFromUI();
		}
	}
	
	//Listen to the Daylight saving check box
	public void OnActivate(){
		if(!dayLightSaving){
			dayLightSaving = true;
			if(widgetStart){
				getInputFromUI();
			}
		}
		else{
			dayLightSaving = false;
			if(widgetStart){
				getInputFromUI();
			}
		}
	}
	
	
	//Listen to the list and change the TimeZone, Longitude, Latitude value
	public void OnSelectionChange(){
		if(cityList.selection == "Philadelphia, PA"){
			timeZoneInput.transform.Find("Label").GetComponent<UILabel>().text = "-5";
			LongitudeInput.transform.Find("Label").GetComponent<UILabel>().text = "-75.1642";
			LatitudeInput.transform.Find("Label").GetComponent<UILabel>().text = "39.9522";
		}
		if(cityList.selection == "Washington, DC"){
			timeZoneInput.transform.Find("Label").GetComponent<UILabel>().text = "-5";
			LongitudeInput.transform.Find("Label").GetComponent<UILabel>().text = "-77.09";
			LatitudeInput.transform.Find("Label").GetComponent<UILabel>().text = "33.89";
		}
		if(cityList.selection == "New York, NY"){
			timeZoneInput.transform.Find("Label").GetComponent<UILabel>().text = "-5";
			LongitudeInput.transform.Find("Label").GetComponent<UILabel>().text = "-74.0064";
			LatitudeInput.transform.Find("Label").GetComponent<UILabel>().text = "40.7142";
		}
		if(cityList.selection == "San Francisco, CA"){
			timeZoneInput.transform.Find("Label").GetComponent<UILabel>().text = "-8";
			LongitudeInput.transform.Find("Label").GetComponent<UILabel>().text = "-122.4183";
			LatitudeInput.transform.Find("Label").GetComponent<UILabel>().text = "37.7750";
		}
		if(selectDateList.selection == "Mar. 20th"){
			monthInput.GetComponentInChildren<UILabel>().text = "03";
			dateInput.GetComponentInChildren<UILabel>().text = "20";
			dayLightSaving = true;
			checkboxScript.isChecked = true;
		}
		if(selectDateList.selection == "Jun. 21st"){
			monthInput.GetComponentInChildren<UILabel>().text = "06";
			dateInput.GetComponentInChildren<UILabel>().text = "21";
			dayLightSaving = true;
			checkboxScript.isChecked = true;

		}
		if(selectDateList.selection == "Sept. 22nd"){
			monthInput.GetComponentInChildren<UILabel>().text = "09";
			dateInput.GetComponentInChildren<UILabel>().text = "22";
			dayLightSaving = true;
			checkboxScript.isChecked = true;
		}
		if(selectDateList.selection == "Dec. 21st"){
			monthInput.GetComponentInChildren<UILabel>().text = "12";
			dateInput.GetComponentInChildren<UILabel>().text = "21";
			dayLightSaving = false;
			checkboxScript.isChecked = false;
		}
		
	}
	
	// !!!cam change the output of digit to Hour: Minutes
	public void OnSliderChange(){
		if(widgetStart){
			getInputFromUI();
			float currentTime = hourSlider.GetComponentInChildren<UISlider>().sliderValue * 24;
			int currentHour = (int)(currentTime);
			int currentMinutes = (int)((currentTime - currentHour)* 60);
			
			string hourNum = currentHour + ": " + currentMinutes;
			hourNumberLabel.text = hourNum;
		}
	}
	
	// the "start widget" button calls this function when clicked.
	public void OnStart(){
	//	if(){
			if(!widgetStart){
				widgetStart = true;
				startButtonLabel.text = "Stop Widget";
				//startButtonSprite.color = new Color(0.76f, 0.41f, 0.41f, 1);
				startButtonSprite.color = Color.red;
				sun.GetComponent<Light>().shadows = LightShadows.Hard;
			}
			else{
				widgetStart = false;
				startButtonLabel.text = "Start Widget";
				startButtonSprite.color = new Color(0.706f, 0.8745f, 0.631f, 1);
				sun.GetComponent<Light>().shadows = LightShadows.None;
			}
			if(widgetStart){
				getInputFromUI();
			}
//		}
	}
	
	
	 //process input from the UI
	public void getInputFromUI(){
			yearStringUIScript = yearInput.GetComponentInChildren<UILabel>();
			yearString = yearStringUIScript.text;
			monthString = monthInput.GetComponentInChildren<UILabel>().text;
			dayString = dateInput.GetComponentInChildren<UILabel>().text;	
			localTime = hourSlider.GetComponentInChildren<UISlider>().sliderValue;	
			longitudeString = LongitudeInput.GetComponentInChildren<UILabel>().text;
			latitudeString = LatitudeInput.GetComponentInChildren<UILabel>().text;
			timeZoneString = timeZoneInput.GetComponentInChildren<UILabel>().text;

			time = new DateTime(int.Parse(yearString), int.Parse(monthString), int.Parse(dayString));
			//time.setYear(float.Parse(yearString));
			//time.setMonth(float.Parse(monthString));
			//time.setDay(float.Parse(dayString));
			//localTime = float.Parse(minutesString);
			longitude = float.Parse(longitudeString);
			latitude = float.Parse(latitudeString);
			timeZone = float.Parse(timeZoneString);
			
		//calcualte the input
			calcSunCoordination();
	}
	
	
	
	//This method calls all the functions needed to calculate the sun position
	public void calcSunCoordination(){
				julianDay = dateToJulian(time);
				julianCentury = calcJulianCentury(time);
				geomMeanAnomSun = calcGeoMeanAnomSun(time);
				eccentEarthOrbit = calcEccentEarthOrbit(time);
				meanObliqEcliptic = calcMeanObliqEcliptic(time);
				ObliqCorr = calcObliqCorr(meanObliqEcliptic);
				varY = calcVarY(ObliqCorr);
				geomMeanLongSun = calcMeanLongSun(julianCentury);
				eqOfTime = calcEqOfTime(geomMeanLongSun,geomMeanAnomSun,eccentEarthOrbit,varY);
				if(!dayLightSaving){
					trueSolarTime = calcTrueSolarTime(localTime,eqOfTime,longitude,timeZone);
				}
				else{
					trueSolarTime = calcTrueSolarTime(localTime - 0.041667f,eqOfTime,longitude,timeZone);
				}
				hourAngle = calcHourAngle(trueSolarTime);
				sunEqOfCtr = calcSunEqOfCtr(julianCentury,geomMeanAnomSun);
				sunTrueLong = calcSunTrueLong(geomMeanLongSun,sunEqOfCtr);
				sunAppLong = calcSunAppLong(sunTrueLong);
				declinationAngle = calcDeclinationAngle(ObliqCorr,sunAppLong);
				zenithAngle = calcZenithAngle(latitude, declinationAngle,hourAngle);
				azimuth = calcAzimuthAngle(hourAngle,latitude,zenithAngle,declinationAngle);
				Debug.Log("azimuth: " + azimuth);
				altitude = calcAltitudeAngle(zenithAngle);
				Debug.Log("altitude: " + altitude);
		//rotate the sun		
		rotateSun(azimuth, altitude);
	}
	
	//PRE: azimuth and altitude angle must be initialized
	//POST: rotate the sun based on the azimuth and altitude angle
	public void rotateSun(float azimuth, float altitude){
		while(azimuth < 0){
			azimuth += 360;
		}
		while (azimuth > 360){
			azimuth -= 360;	
		}
		//set azimuth angle
		sun.eulerAngles = new Vector3(0, azimuth - 180, 0);
		//set altitude angle
		sun.eulerAngles = new Vector3(altitude, sun.eulerAngles.y, sun.eulerAngles.z);
		 
	}
	
	//converts the current date to Julianday
	public float dateToJulian(DateTime time){
		float month = time.Month;
		float day = time.Day;
		float year = time.Year;
		
		if(month < 3){
			month = month + 12;
			year = year -1;
		}
		
		// NOTE that julianDay here loses precision. It is truncated into an int after adding "1721119". The float or double cannot hold decimals when storing
		//numbers with more than 7 digits.
		float julianDay = day + (153.0f * month - 457.0f) / 5.0f + 365.0f * year + (year/4.0f) - (year / 100.0f) + (year/400.0f) + 1721119.0f + localTime - timeZone/24.0f;

		//print out julianDay
		Debug.Log(julianDay);
		return julianDay;
	}
	
	
	public float calcAzimuthAngle(float hourAngle, float latitude, float zenithAngle, float declinationAngle){
		float radLatitude = latitude * Mathf.Deg2Rad;
		float radZenithAngle = zenithAngle * Mathf.Deg2Rad;
		float radDeclinationAngle = declinationAngle * Mathf.Deg2Rad;
		float sinRadLatitude = Mathf.Sin(radLatitude);
		float sinRadDeclinationAngle = Mathf.Sin(radDeclinationAngle);
		float sinRadZenithAngle = Mathf.Sin(radZenithAngle);
		float cosRadLatitude = Mathf.Cos(radLatitude);
	//	float cosRadDeclinationAngle = Mathf.Cos(radDeclinationAngle);
		float cosRadZenithAngle = Mathf.Cos(radZenithAngle);
		float azimuth;
		if(hourAngle > 0){
			 azimuth = 
			(Mathf.Acos(((sinRadLatitude * cosRadZenithAngle) - sinRadDeclinationAngle)/(cosRadLatitude * sinRadZenithAngle))*Mathf.Rad2Deg + 180) % 360;
		}
		else{
			 azimuth = 
			(540 - Mathf.Acos(((sinRadLatitude * cosRadZenithAngle)- sinRadDeclinationAngle)/ (cosRadLatitude * sinRadZenithAngle))*Mathf.Rad2Deg) % 360;
		}
		Debug.Log("");
		return azimuth;
	}
	
	public float calcAltitudeAngle(float zenithAngle){
		return 90-zenithAngle;
	}
		
	public float calcHourAngle(float trueSolarTime){
		float hourAngle;
		if(trueSolarTime /4 < 0){
			hourAngle = trueSolarTime/4 + 180;
		}
		else{
			hourAngle = trueSolarTime/4 -180;
		}
		Debug.Log("");
		return hourAngle;
	}
	
	public float calcTrueSolarTime(float localTime, float eqOfTime, float longitude, float timeZone){
		float trueSolarTime1;
		if((localTime * 1440 + eqOfTime + 4* longitude - 60* timeZone) < 0){
			trueSolarTime1 = (localTime * 1440 + eqOfTime + 4* longitude - 60* timeZone) - 1440 * Mathf.Floor((localTime * 1440 + eqOfTime + 4* longitude - 60* timeZone)/1440);
		}
		else{
			trueSolarTime1 = ((localTime * 1440 + eqOfTime + 4* longitude - 60* timeZone) % 1440);
		}
		Debug.Log("");
		return trueSolarTime1;
	}
	
	public float calcEqOfTime(float geomMeanLongSun, float geomMeanAnomSun, float eccentEarthOrbit, float varY){
		float radMeanLongSun = geomMeanLongSun * Mathf.Deg2Rad;
		float radGeoMeanAnomSun = geomMeanAnomSun * Mathf.Deg2Rad;
		float eqOfTime;
		
		eqOfTime = 
			4*(varY * Mathf.Sin(2 * radMeanLongSun) - 2 * eccentEarthOrbit * Mathf.Sin(radGeoMeanAnomSun) + 4 * eccentEarthOrbit * varY * Mathf.Sin(radGeoMeanAnomSun) * Mathf.Cos(2 * radMeanLongSun) - 0.5f *varY * varY*Mathf.Sin(4 * radMeanLongSun) - 1.25f * eccentEarthOrbit * eccentEarthOrbit * Mathf.Sin(2 * radGeoMeanAnomSun))*Mathf.Rad2Deg;
		Debug.Log("");
		return eqOfTime;
	}
	
	public float calcMeanLongSun(float julianCentury){
		float geomMeanLongSun = (280.46646f + julianCentury * (36000.76983f + julianCentury * 0.0003032f))% 360;
		Debug.Log("");
		return geomMeanLongSun;
		
	}
		
	public float calcJulianCentury(DateTime time){
		float julianCentury = (dateToJulian(time) - 2451545) / 36525;
		Debug.Log("");
		return julianCentury;
	}
	
	public float calcVarY(float obliqCorr){
		float varY = Mathf.Tan((obliqCorr/2)*Mathf.Deg2Rad)*Mathf.Tan((obliqCorr/2)*Mathf.Deg2Rad);
		Debug.Log("");
		return varY;
	}
	
	public float calcObliqCorr(float meanObliqEcliptic){
		float obliqCorr = 
			meanObliqEcliptic + 0.00256f * Mathf.Cos((125.04f - 1934.136f * calcJulianCentury(time))*Mathf.Deg2Rad);
		Debug.Log("");
		return obliqCorr;
	}
	
	public float calcMeanObliqEcliptic(DateTime time){
		float julianCentury = calcJulianCentury(time);
		float meanObliqEcliptic = 23+(26+((21.448f-julianCentury*(46.815f+julianCentury*(0.00059f - julianCentury * 0.001813f))))/60)/60;
		Debug.Log("");
		return meanObliqEcliptic; 
	}
	
	public float calcEccentEarthOrbit(DateTime time){
		float julianCentury = calcJulianCentury(time);
		float eccenEarthOrbit = 0.016708634f - julianCentury * (0.000042037f+0.0000001267f* julianCentury);
		Debug.Log("");
		return eccenEarthOrbit;
	}
	
	public float calcGeoMeanAnomSun(DateTime time){
		float julianCentury = calcJulianCentury(time);
		float geoMeanAnomSun = 357.52911f+julianCentury*(35999.05029f - 0.0001537f*julianCentury);
		Debug.Log("");
		return geoMeanAnomSun;
	}
	
	public float calcDeclinationAngle(float obliqCorr, float sunAppLong){
		float declinationAngle =  Mathf.Asin(Mathf.Sin(obliqCorr* Mathf.Deg2Rad)* Mathf.Sin(sunAppLong* Mathf.Deg2Rad))*Mathf.Rad2Deg;
		Debug.Log("");
		return declinationAngle;
	}
	
	public float calcSunAppLong(float sunTrueLong){
		float sunAppLong = sunTrueLong - 0.00569f-0.00478f*Mathf.Sin((125.04f-1934.136f*this.julianCentury)*Mathf.Deg2Rad);
		Debug.Log("");
		return sunAppLong;
	}
	
	public float calcSunTrueLong(float geomMeanLongSun, float sunEqOfCtr){
		Debug.Log("");
		return geomMeanLongSun + sunEqOfCtr;
	}
	
	public float calcSunEqOfCtr(float julianCentury, float geomMeanAnomSun){
		Debug.Log("");
		return (Mathf.Sin(geomMeanAnomSun*Mathf.Deg2Rad)*(1.914602f - julianCentury*(0.004817f+0.000014f*julianCentury)) + 
			Mathf.Sin(geomMeanAnomSun *2 *Mathf.Deg2Rad)*(0.019993f-0.000101f*julianCentury) + Mathf.Sin(3*geomMeanAnomSun*Mathf.Deg2Rad* 0.000289f));
	}
	
	public float calcZenithAngle(float latitude, float declinationAngle, float hourAngle){
		Debug.Log("");
		return ((Mathf.Acos(Mathf.Sin(latitude*Mathf.Deg2Rad)*Mathf.Sin(declinationAngle *Mathf.Deg2Rad)+ 
			Mathf.Cos(latitude*Mathf.Deg2Rad)*Mathf.Cos(declinationAngle*Mathf.Deg2Rad)*Mathf.Cos(hourAngle*Mathf.Deg2Rad)))*Mathf.Rad2Deg);	
	}
	
}
	