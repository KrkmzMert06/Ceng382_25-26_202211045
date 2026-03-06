function getLetterGrade(avg){

if(avg>=90) return "AA";
if(avg>=85) return "BA";
if(avg>=80) return "BB";
if(avg>=70) return "CB";
if(avg>=60) return "CC";
if(avg>=50) return "DD";

return "FF";
}

function showGrades(){

let rows=document.querySelectorAll("#students tr");

rows.forEach(row=>{

let midterm=parseFloat(row.querySelector(".midterm").textContent);
let final=parseFloat(row.querySelector(".final").textContent);

let avg=(midterm*0.4)+(final*0.6);

let letter=getLetterGrade(avg);

row.querySelector(".letter").textContent=letter;

});

}

document.getElementById("showGradesBtn").addEventListener("click",showGrades);