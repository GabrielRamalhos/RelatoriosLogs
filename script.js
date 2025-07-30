// Selecione todas as tabelas da página
const tables = document.querySelectorAll('table');

tables.forEach((table, index) => {
  if (index !== tables.length - 1) {
    const colgroup = document.createElement('colgroup');
    colgroup.classList.add('th-head');

    const col1 = document.createElement('col');
    col1.style.width = '25%';
    colgroup.appendChild(col1);

    const col2 = document.createElement('col');
    col2.style.width = '15%';
    colgroup.appendChild(col2);

    const col3 = document.createElement('col');
    col3.style.width = '45%';
    colgroup.appendChild(col3);

    const col4 = document.createElement('col');
    col4.style.width = '15%';
    colgroup.appendChild(col4);

    table.insertBefore(colgroup, table.firstChild);
  }
});