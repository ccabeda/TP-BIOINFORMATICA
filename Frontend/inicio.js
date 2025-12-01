//VALIDACIÓN DE ARCHIVOS
function validarArchivos(f1, f2) {
    if (!f1 || !f2) return "Debés subir ambos archivos."; //falta archivo

    const ext1 = f1.name.toLowerCase(); //nombre del archivo en minúscula
    const ext2 = f2.name.toLowerCase();

    const esFasta1 = ext1.endsWith(".fasta") || ext1.endsWith(".fa"); //verifica si el archivo es fasta
    const esFasta2 = ext2.endsWith(".fasta") || ext2.endsWith(".fa");

    const esPdb1 = ext1.endsWith(".pdb"); //verifica si el archivo es pdb
    const esPdb2 = ext2.endsWith(".pdb");

    //formato incorrecto
    if (!esFasta1 && !esPdb1) return "El archivo 1 tiene un formato incorrecto. Solo FASTA o PDB."; //
    if (!esFasta2 && !esPdb2) return "El archivo 2 tiene un formato incorrecto. Solo FASTA o PDB.";

    //formatos diferentes
    const tipo1 = esFasta1 ? "fasta" : "pdb";
    const tipo2 = esFasta2 ? "fasta" : "pdb";
    if (tipo1 !== tipo2)
        return "Los archivos deben ser del MISMO tipo (ambos FASTA o ambos PDB).";
    return null; // OK
}

//BOTONES INFO PDB 
function crearBotonesInfo(data) {
    const cont = document.getElementById("infoButtons"); //creo botones de info

    const extraerPDB = (idRaw) => { //extrae el id pdb del nombre del archivo para llevar a la pagina oficial
        let token = idRaw.split(" ")[0];
        token = token.split("_")[0].split("-")[0].split(".")[0].split("|")[0];
        if (/^[A-Za-z0-9]{4}$/.test(token)) return token.toUpperCase(); //valida que tenga 4 caracteres alfanuméricos
        return null;
    };

    const id1 = extraerPDB(data.id1);
    const id2 = extraerPDB(data.id2);
    let html = ""; // HTML de los botones
    //plantilla de los botones
    const botonInfo = (id) => ` 
        <a target="_blank" 
            href="https://www.rcsb.org/structure/${id}" 
            class="btn btn-success me-2 mb-2">
            🔬 Info ${id}
        </a>
    `;

    const boton3D = (id) => `
        <a target="_blank" 
            href="https://www.rcsb.org/3d-view/${id}/1" 
            class="btn btn-success mb-2">
            🧬 Ver 3D ${id}
        </a>
    `;

    if (id1) { //
        html += botonInfo(id1) + boton3D(id1) + "<br>"; //
    }
    if (id2) {
        html += botonInfo(id2) + boton3D(id2);
    }
    cont.innerHTML = html || "";
}

// ENVIAR ARCHIVOS Y RECIBIR RESPUESTA
async function sendFiles() {
    const f1 = document.getElementById("file1").files[0];
    const f2 = document.getElementById("file2").files[0];
    const windowSize = document.getElementById("window").value || 9;
    const status = document.getElementById("status");

    //validación frontend
    const error = validarArchivos(f1, f2);
    if (error) {
        status.innerHTML = "❌ " + error;
        return;
    }

    status.innerHTML = "Procesando...";
    const form = new FormData();
    form.append("file1", f1);
    form.append("file2", f2);

    try {
        const res = await fetch(
            `https://localhost:7215/api/DotPlot/dotplot?window=${windowSize}`,
            { method: "POST", body: form }
        );
        if (!res.ok) {
    const err = await res.json();
    status.innerHTML = "❌ " + err.message;
    return;
        }

        const data = await res.json();
        status.innerHTML = `Completado ✔ — Matches: ${data.sharedSections}`;

        drawPlot(data, windowSize);

        //crea los botones de info PDB
        crearBotonesInfo(data);

    } catch (e) {
        console.error(e);
        status.innerHTML = "❌ Error al procesar.";
    }
}

// CREAR GRÁFICO
function drawPlot(data, windowSize) {
    const maxLen = Math.max(data.seq1.length, data.seq2.length); //longitud máxima entre las dos secuencias

    const scatter = { //datos del gráfico
        x: data.x, //coordenadas x e y de los puntos
        y: data.y,
        mode: "markers", //tipo de gráfico
        type: "scatter", //gráfico de dispersión
        marker: { size: 4, color: "green" } //tamaño y color de los puntos
    };
    const layout = { //configuración del gráfico
        title: {
            text: `DotPlot (window = ${windowSize})`,
            x: 0.5,
            y: 1.05,
            font: { size: 24 }
        },
        width: 750,
        height: 750,
        xaxis: { //configuración del eje x
            title: {
                text: `${data.id2} (Longitud ${data.seq2.length} aa)`,
                font: { size: 20 },
                standoff: 20
            },
            range: [0, maxLen],
            tickfont: { size: 14 },
            mirror: true,
            linecolor: "#333",
            linewidth: 2,
            showgrid: true,
            gridcolor: "rgba(0,0,0,0.1)"
        },
        yaxis: { //configuración del eje y
            title: {
                text: `${data.id1} (Longitud ${data.seq1.length} aa)`,
                font: { size: 20 },
                standoff: 20
            },
            range: [maxLen, 0],
            tickfont: { size: 14 },
            mirror: true,
            linecolor: "#333",
            linewidth: 2,
            showgrid: true,
            gridcolor: "rgba(0,0,0,0.1)"
        },
        margin: { l: 130, r: 30, t: 90, b: 120 }, //márgenes del gráfico
        showlegend: false
    };
    Plotly.newPlot("grafico", [scatter], layout); //dibuja el gráfico en el div con id "grafico"
}

// MODO OSCURO
document.addEventListener("DOMContentLoaded", () => {
    // crea HTML del botón
    const toggleHTML = `
        <div class="darkmode-switch" id="switchDM">
            <div class="darkmode-circle" id="circleDM">☀️</div>
        </div>
    `;
    document.getElementById("darkmode-toggle").innerHTML = toggleHTML;

    const switchDM = document.getElementById("switchDM");
    const circleDM = document.getElementById("circleDM");

    //si estaba guardado en localStorage → activarlo
    const modoGuardado = localStorage.getItem("modo");
    if (modoGuardado === "oscuro") {
        document.documentElement.classList.add("dark");
        switchDM.classList.add("dark");
        circleDM.textContent = "🌙";
    }
    // evento de click
    switchDM.addEventListener("click", () => {
        document.documentElement.classList.toggle("dark");
        switchDM.classList.toggle("dark");

        const oscuro = document.documentElement.classList.contains("dark");

        circleDM.textContent = oscuro ? "🌙" : "☀️";
        //guardar preferencia
        localStorage.setItem("modo", oscuro ? "oscuro" : "claro");
    });
});
