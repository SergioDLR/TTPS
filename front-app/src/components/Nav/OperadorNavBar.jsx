import * as React from "react";
import AppBar from "@mui/material/AppBar";
import Toolbar from "@mui/material/Toolbar";
import CssBaseline from "@mui/material/CssBaseline";
import { Button } from "@mui/material";
import { Link } from "react-router-dom";
import { UserContext } from "../Context/UserContext";
const OperadorNavBar = () => {
  const { cerrarSesion } = React.useContext(UserContext);
  return (
    <>
      <CssBaseline />
      <AppBar sx={{ backgroundColor: "black" }}>
        <Toolbar>
          <Link to={"/operador/vacunas"} style={{ textDecoration: "none" }}>
            <Button variant="contained">Vacunas</Button>
          </Link>
          <Button sx={{ backgroundColor: "red", marginLeft: 1 }} onClick={cerrarSesion} variant="contained">
            Cerrar sesion
          </Button>
        </Toolbar>
      </AppBar>
      <Toolbar />
    </>
  );
};

export default OperadorNavBar;
